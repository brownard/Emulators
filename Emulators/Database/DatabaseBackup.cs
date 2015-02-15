using Emulators.ImageHandlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Emulators.Database
{
    #region EventArgs

    public class BackupErrorEventArgs : EventArgs
    {
        public BackupErrorEventArgs(DataErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }

        public DataErrorType ErrorType { get; protected set; }
        public string Message { get; protected set; }
    }

    public class BackupProgressEventArgs : EventArgs
    {
        public BackupProgressEventArgs(int percent, int current, int total, string message)
        {
            Percent = percent;
            Current = current;
            Total = total;
            Message = message;
        }

        public int Percent { get; protected set; }
        public int Current { get; protected set; }
        public int Total { get; protected set; }
        public string Message { get; protected set; }
    }

    #endregion

    public enum DataErrorType
    {
        LoadFile,
        InvalidData,
        SQLError,
        SaveFile,
        InvalidVersion
    }
    public enum MergeType
    {
        Create,
        Ignore,
        Merge
    }

    /// <summary>
    /// Provides methods for backing up or restoring the database
    /// </summary>
    public class DatabaseBackup
    {
        public event EventHandler<BackupErrorEventArgs> BackupError; //error event
        protected virtual void OnBackupError(BackupErrorEventArgs e)
        {
            if (BackupError != null)
                BackupError(this, e);
        }
        public event EventHandler<BackupProgressEventArgs> BackupProgress; //progress event
        protected virtual void OnBackupProgress(BackupProgressEventArgs e)
        {
            if (BackupProgress != null)
                BackupProgress(this, e);
        }
        public event EventHandler Completed;
        protected virtual void OnCompleted()
        {
            if (Completed != null)
                Completed(this, EventArgs.Empty); 
        }

        bool backupThumbs = true;
        public bool BackupThumbs
        {
            get { return backupThumbs; }
            set { backupThumbs = value; }
        }
        bool restoreThumbs = true;
        public bool RestoreThumbs
        {
            get { return restoreThumbs; }
            set { restoreThumbs = value; }
        }
        MergeType emulatorMergeType = MergeType.Merge;
        public MergeType EmulatorMergeType
        {
            get { return emulatorMergeType; }
            set { emulatorMergeType = value; }
        }
        MergeType gameMergeType = MergeType.Merge;
        public MergeType GameMergeType
        {
            get { return gameMergeType; }
            set { gameMergeType = value; }
        }
        public bool CleanRestore { get; set; }

        #region Backup
        /// <summary>
        /// Backup the database to the specified save path
        /// </summary>
        /// <param name="savePath">The path where the backup xml will be created</param>
        public void Backup(string savePath)
        {
            string backupDirectory;
            try
            {
                backupDirectory = System.IO.Path.GetDirectoryName(savePath);
                if (!Directory.Exists(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);
            }
            catch
            {
                //error creating save path
                OnBackupError(new BackupErrorEventArgs(DataErrorType.SaveFile, string.Format("Unable to create specified save path '{0}'.", savePath)));
                return;
            }
            
            if (backupThumbs)
            {
                backupDirectory += string.Format(@"\{0}_Thumbs", Path.GetFileNameWithoutExtension(savePath));
                if (!Directory.Exists(backupDirectory))
                {
                    try { Directory.CreateDirectory(backupDirectory); }
                    catch (Exception ex)
                    {
                        Logger.LogError("Unable to create backup thumb directory '{0}' - {1}", backupDirectory, ex.Message);
                        OnBackupError(new BackupErrorEventArgs(DataErrorType.SaveFile, string.Format("Unable to create backup thumb directory '{0}'.", backupDirectory)));
                        return;
                    }
                }
            }

            //create xml doc
            XmlDocument doc = new XmlDocument();
            //add declaration
            XmlNode headNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(headNode);
            //add top node
            XmlNode docNode = doc.CreateElement("backup");
            doc.AppendChild(docNode);
            //add db version info
            XmlNode dbVersion = doc.CreateElement("version");
            XmlAttribute attr = doc.CreateAttribute("value");
            attr.Value = DB.DB_VERSION.ToString(new CultureInfo("en-US", false));
            dbVersion.Attributes.Append(attr);
            docNode.AppendChild(dbVersion);

            lock (EmulatorsCore.Database.SyncRoot)
            {
                createNodes(docNode, Emulator.GetAll(), backupDirectory, 0); //add emu nodes
                createNodes(docNode, Game.GetAll(), backupDirectory, 50); //add game nodes
            }
            OnBackupProgress(new BackupProgressEventArgs(100, 2, 2, "Saving..."));
            try { doc.Save(savePath); }
            catch (Exception ex) //error saving doc
            {
                OnBackupError(new BackupErrorEventArgs(DataErrorType.SaveFile, ex.Message));
                return;
            }
            OnCompleted();
        }

        /// <summary>
        /// Creates xml nodes from database entries based on the specified DataType
        /// and appends them to the specified docNode
        /// </summary>
        /// <param name="doc">The parent xml documnet</param>
        /// <param name="docNode">The node to append the newly created items</param>
        /// <param name="itemType"></param>
        void createNodes<T>(XmlNode docNode, List<T> items, string thumbDirectory, int startPerc) where T : DBItem
        {
            int total = items.Count;
            if (total < 1)
                return;
            string typeName = typeof(T).Name;
            XmlNode parent = docNode.OwnerDocument.CreateElement(typeName + "s");
            docNode.AppendChild(parent);

            string info = string.Format("Saving {0}...", typeName);
            double perc = startPerc;
            double increment = 50.0 / total;
            OnBackupProgress(new BackupProgressEventArgs(startPerc, 0, total, info));

            ReadOnlyCollection<DBField> fieldList = DBField.GetFieldList(typeof(T));
            //loop through results
            for (int x = 0; x < total; x++)
            {
                OnBackupProgress(new BackupProgressEventArgs((int)perc, x + 1, total, info));
                perc += increment;
                addNode(parent, items[x], thumbDirectory);
            }
        }

        void addNode(XmlNode parentNode, DBItem dbItem, string thumbDirectory)
        {
            //create item node
            XmlElement xmlItem = parentNode.OwnerDocument.CreateElement(dbItem.GetType().FullName);
            parentNode.AppendChild(xmlItem);
            XmlElement idNode = parentNode.OwnerDocument.CreateElement("Id");
            idNode.InnerText = dbItem.Id.ToString();
            xmlItem.AppendChild(idNode);
            //loop through each field
            foreach (DBField field in DBField.GetFieldList(dbItem.GetType()))
            {
                XmlElement fieldNode = parentNode.OwnerDocument.CreateElement(field.Name);
                string val = DB.GetString(field, field.GetValue(dbItem));
                if (val == null)
                    val = "";
                else if (val == "")
                    val = " ";
                fieldNode.InnerText = val;
                xmlItem.AppendChild(fieldNode);
            }

            ReadOnlyCollection<DBRelation> relations = DBRelation.GetRelations(dbItem.GetType());
            if (relations.Count > 0)
            {
                XmlElement relationsNode = xmlItem.OwnerDocument.CreateElement("Relations");
                xmlItem.AppendChild(relationsNode);
                foreach (DBRelation currRelation in relations)
                {
                    IRelationList relationList = currRelation.GetRelationList(dbItem);
                    if (relationList.Count < 1)
                        continue;
                    XmlElement relationNode = relationsNode.OwnerDocument.CreateElement(currRelation.PropertyInfo.Name);
                    relationsNode.AppendChild(relationNode);
                    foreach (DBItem relation in relationList)
                        addNode(relationNode, relation, thumbDirectory);
                }
            }

            if (backupThumbs)
            {
                ThumbItem thumbItem = dbItem as ThumbItem;
                if (thumbItem != null)
                    copyThumbs(thumbItem, thumbDirectory);
            }
        }

        void copyThumbs(ThumbItem item, string saveDir)
        {
            using (ThumbGroup thumbGroup = new ThumbGroup(item))
            {
                if (!Directory.Exists(thumbGroup.ThumbPath))
                    return;

                string newDir = string.Format(@"{0}\{1}\{2}", saveDir, item.ThumbFolder, item.Id);
                if (!Directory.Exists(newDir))
                {
                    try { Directory.CreateDirectory(newDir); }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error creating backup thumb directory '{0}' - {1}", newDir, ex.Message);
                        return;
                    }
                }

                string manualFile = thumbGroup.ManualPath;
                if (!string.IsNullOrEmpty(manualFile) && File.Exists(manualFile))
                    copyThumb(manualFile, newDir);

                string thumbFile;
                if (thumbGroup.HasLocalThumb(ThumbType.Fanart, out thumbFile))
                    copyThumb(thumbFile, newDir);

                if (item.HasGameArt)
                {
                    if (thumbGroup.HasLocalThumb(ThumbType.FrontCover, out thumbFile))
                        copyThumb(thumbFile, newDir);
                    if (thumbGroup.HasLocalThumb(ThumbType.BackCover, out thumbFile))
                        copyThumb(thumbFile, newDir);
                    if (thumbGroup.HasLocalThumb(ThumbType.TitleScreen, out thumbFile))
                        copyThumb(thumbFile, newDir);
                    if (thumbGroup.HasLocalThumb(ThumbType.InGameScreen, out thumbFile))
                        copyThumb(thumbFile, newDir);
                }
                else
                {
                    if (thumbGroup.HasLocalThumb(ThumbType.Logo, out thumbFile))
                        copyThumb(thumbFile, newDir);
                }
            }
        }

        void copyThumb(string file, string path)
        {
            try
            {
                File.Copy(file, Path.Combine(path, Path.GetFileName(file)), true);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error copying thumb '{0}' to backup directory '{1}' - {2}", file, path, ex.Message);
            }
        }

        #endregion

        #region Restore

        string thumbDir = null;
        DatabaseCache restoreItems;
        Dictionary<int, Emulator> updatedEmulators;
        Dictionary<string, Emulator> dbEmulators;
        Dictionary<string, Game> dbGames;
        /// <summary>
        /// Restore the database from the specified xml file using the specified merge settings
        /// </summary>
        /// <param name="xmlPath">The path to the xml file containing backup data</param>
        public void Restore(string xmlPath)
        {
            if (!File.Exists(xmlPath)) //error locating specified file
            {
                OnBackupError(new BackupErrorEventArgs(DataErrorType.LoadFile, "Unable to locate specified backup file."));
                return;
            }

            //create xml from file
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(xmlPath);
            }
            catch (Exception ex)
            {
                //error creating xml
                Logger.LogError("Error loading specified backup - {0}", ex.Message);
                OnBackupError(new BackupErrorEventArgs(DataErrorType.LoadFile, ex.Message));
                return;
            }

            XmlNodeList versions = doc.GetElementsByTagName("version");
            double version;
            if (versions.Count < 1 || versions[0].Attributes["value"] == null || !double.TryParse(versions[0].Attributes["value"].Value, System.Globalization.NumberStyles.Any, new CultureInfo("en-US", false), out version))
            {
                OnBackupError(new BackupErrorEventArgs(DataErrorType.InvalidVersion, "No version info found for backup"));
                return;
            }
            if (version < 2.0)
            {
                OnBackupError(new BackupErrorEventArgs(DataErrorType.InvalidVersion, "Backups from database versions lower than 2.0 are not supported"));
                return;
            }
            else if (version > DB.DB_VERSION)
            {
                OnBackupError(new BackupErrorEventArgs(DataErrorType.InvalidVersion, "This backup is from a newer version of Emulators 2 and is not supported"));
                return;
            }

            if (restoreThumbs)
            {
                thumbDir = Path.GetDirectoryName(xmlPath) + string.Format(@"\{0}_Thumbs", Path.GetFileNameWithoutExtension(xmlPath));
                if (!Directory.Exists(thumbDir))
                {
                    Logger.LogWarn("Unable to locate thumb backup folder '{0}'", thumbDir);
                    thumbDir = null;
                }
            }

            restoreItems = new DatabaseCache();
            updatedEmulators = new Dictionary<int, Emulator>();
            lock (EmulatorsCore.Database.SyncRoot)
            {
                if (CleanRestore)
                {
                    Logger.LogInfo("Clean restore - deleting all existing data");
                    OnBackupProgress(new BackupProgressEventArgs(0, 0, 0, "Cleaning Database"));
                    List<Emulator> existingItems = Emulator.GetAll();
                    EmulatorsCore.Database.BeginTransaction();
                    foreach (Emulator emu in existingItems)
                    {
                        emu.Delete();
                    }
                    EmulatorsCore.Database.EndTransaction();
                    dbEmulators = new Dictionary<string, Emulator>();
                    dbGames = new Dictionary<string, Game>();
                }
                else
                {
                    dbEmulators = getEmulatorNames();
                    dbGames = getGamePaths();
                }

                List<DBItem> emulators = getItems(doc.GetElementsByTagName(typeof(Emulator).FullName), true);
                List<DBItem> games = getItems(doc.GetElementsByTagName(typeof(Game).FullName), true);
                int total = emulators.Count + games.Count;
                int current = 1;

                EmulatorsCore.Database.BeginTransaction();
                foreach (Emulator emu in emulators)
                {
                    OnBackupProgress(new BackupProgressEventArgs((current * 100) / total, current, total, "Restoring " + emu.Title));
                    current++;
                    updateEmulator(emu);
                }
                foreach (Game game in games)
                {
                    OnBackupProgress(new BackupProgressEventArgs((current * 100) / total, current, total, "Restoring " + game.Title));
                    current++;
                    updateGame(game);
                }
                EmulatorsCore.Database.EndTransaction();
            }
            OnCompleted();
        }

        List<DBItem> getItems(XmlNodeList xmlItems, bool reportProgress)
        {
            List<DBItem> items = new List<DBItem>();
            if (xmlItems.Count < 1)
                return items;

            Type itemType;
            try { itemType = Type.GetType(xmlItems[0].Name); }
            catch(Exception ex)
            {
                Logger.LogError("Failed to get Type with name {0} - {1}", xmlItems[0].Name, ex.Message);
                return items;
            }

            int totalItems = xmlItems.Count;
            int currentItem = 1;
            foreach (XmlNode xmlItem in xmlItems)
            {
                if (reportProgress)
                    OnBackupProgress(new BackupProgressEventArgs(0, currentItem, totalItems, "Parsing " + itemType.Name));
                currentItem++;

                DBItem dbItem = (DBItem)itemType.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                List<XmlNode> propertyNodes = new List<XmlNode>();
                XmlNode relationsNode = null;
                foreach (XmlNode node in xmlItem.ChildNodes)
                {
                    if (node.Name == "Id")
                        dbItem.Id = int.Parse(node.InnerText);
                    else if (node.Name == "Relations")
                        relationsNode = node;
                    else
                        propertyNodes.Add(node);
                }

                if (dbItem.Id == null)
                {
                    Logger.LogWarn("Skipping item - missing Id");
                    continue;
                }
                
                if (relationsNode != null)
                    foreach (XmlNode relationNode in relationsNode.ChildNodes)
                        populateRelationList(dbItem, relationNode);

                foreach (XmlNode propertyNode in propertyNodes)
                    populateProperties(dbItem, propertyNode);

                items.Add(dbItem);
                restoreItems.Add(dbItem);
            }
            return items;
        }

        void populateProperties(DBItem owner, XmlNode propertyNode)
        {
            DBField field = DBField.GetField(owner.GetType(), propertyNode.Name);
            if (field != null)
            {
                object val = null;
                if (field.DBType == DBDataType.DB_OBJECT)
                {
                    int id;
                    if (int.TryParse(propertyNode.InnerText, out id))
                        val = restoreItems.Get(field.Type, id);
                }
                else if (!string.IsNullOrEmpty(propertyNode.InnerText))
                {
                    val = propertyNode.InnerText;
                }

                field.SetValue(owner, val);
            }
        }

        void populateRelationList(DBItem owner, XmlNode relationNode)
        {
            DBRelation relationInfo = DBRelation.GetRelation(owner.GetType(), relationNode.Name);
            if (relationInfo != null)
            {
                IRelationList relationList = relationInfo.GetRelationList(owner);
                List<DBItem> relations = getItems(relationNode.ChildNodes, false);
                foreach (DBItem item in relations)
                {
                    relationList.AddDBItem(item);
                    restoreItems.Add(item);
                }
            }
        }

        void updateEmulator(Emulator emu)
        {
            Emulator dbEmulator = null;
            if (emu.IsPc() || (emulatorMergeType != MergeType.Create && dbEmulators.TryGetValue(emu.Title, out dbEmulator)))
            {
                if (dbEmulator == null)
                {
                    dbEmulator = Emulator.GetPC();
                    if (dbEmulator == null)
                        dbEmulator = emu;
                }
                updatedEmulators.Add(emu.Id.Value, dbEmulator);
                if (emulatorMergeType == MergeType.Merge)
                {
                    Logger.LogDebug("Merging existing emulator with backup emulator '{0}'", emu.Title);
                    mergeEmulator(emu, dbEmulator);
                    mergeThumbs(dbEmulator, emu.Id.Value);
                    dbEmulator.Commit();
                }
                else Logger.LogDebug("Ignoring backup emulator '{0}', an emulator with the same name already exists", emu.Title);
            }
            else
            {
                int oldId = emu.Id.Value;
                emu.Id = null;
                foreach (EmulatorProfile profile in emu.EmulatorProfiles)
                    profile.Id = null;
                emu.Commit();
                mergeThumbs(emu, oldId);
            }
        }

        void updateGame(Game game)
        {
            Game dbGame = null;
            foreach (GameDisc disc in game.Discs)
            {
                if (dbGame == null && dbGames.ContainsKey(disc.Path))
                    dbGame = dbGames[disc.Path];
                disc.Id = null;
            }

            if (dbGame != null)
            {
                if (gameMergeType == MergeType.Merge)
                {
                    Logger.LogDebug("Merging existing game with backup game '{0}'", game.Title);
                    mergeGame(game, dbGame);
                    mergeThumbs(dbGame, game.Id.Value);
                    dbGame.Commit();
                }
                else Logger.LogDebug("Ignoring backup game '{0}', a game with the same path already exists", game.Title);
            }
            else
            {
                Emulator parentEmulator;
                if (updatedEmulators.TryGetValue(game.ParentEmulator.Id.Value, out parentEmulator))
                {
                    game.ParentEmulator = parentEmulator;
                    if (!parentEmulator.IsPc())
                        game.CurrentProfile = null;
                }
                int oldId = game.Id.Value;
                game.Id = null;
                foreach (EmulatorProfile profile in game.GameProfiles)
                    profile.Id = null;
                game.Commit();
                mergeThumbs(game, oldId);
            }
        }

        static void mergeEmulator(Emulator oldEmulator, Emulator newEmulator)
        {
            if (oldEmulator.IsPc())
                newEmulator.Title = oldEmulator.Title;
            if (string.IsNullOrEmpty(newEmulator.Description) && !string.IsNullOrEmpty(oldEmulator.Description))
                newEmulator.Description = oldEmulator.Description;
            if (string.IsNullOrEmpty(newEmulator.Developer) && !string.IsNullOrEmpty(oldEmulator.Developer))
                newEmulator.Developer = oldEmulator.Developer;
            if (string.IsNullOrEmpty(newEmulator.VideoPreview) && !string.IsNullOrEmpty(oldEmulator.VideoPreview))
                newEmulator.VideoPreview = oldEmulator.VideoPreview;
            if (newEmulator.Grade == 0 && oldEmulator.Grade != 0)
                newEmulator.Grade = oldEmulator.Grade;
            if (newEmulator.Year == 0 && oldEmulator.Year != 0)
                newEmulator.Year = oldEmulator.Year;
        }

        static void mergeGame(Game oldGame, Game newGame)
        {
            bool hasInfo = false;
            if (string.IsNullOrEmpty(newGame.Description) && !string.IsNullOrEmpty(oldGame.Description))
            {
                newGame.Description = oldGame.Description;
                hasInfo = true;
            }
            if (string.IsNullOrEmpty(newGame.Developer) && !string.IsNullOrEmpty(oldGame.Developer))
            {
                newGame.Developer = oldGame.Developer;
                hasInfo = true;
            }
            if (string.IsNullOrEmpty(newGame.Genre) && !string.IsNullOrEmpty(oldGame.Genre))
            {
                newGame.Genre = oldGame.Genre;
                hasInfo = true;
            }
            if (newGame.Grade == 0 && oldGame.Grade != 0)
            {
                newGame.Grade = oldGame.Grade;
                hasInfo = true;
            }
            if (newGame.Year == 0 && oldGame.Year != 0)
            {
                newGame.Year = oldGame.Year;
                hasInfo = true;
            }
            if (hasInfo)
                newGame.Title = oldGame.Title;
            if (string.IsNullOrEmpty(newGame.VideoPreview) && !string.IsNullOrEmpty(oldGame.VideoPreview))
                newGame.VideoPreview = oldGame.VideoPreview;
            if (newGame.Latestplay == DateTime.MinValue && oldGame.Latestplay != DateTime.MinValue)
                newGame.Latestplay = oldGame.Latestplay;            
        }

        void mergeThumbs(ThumbItem thumbItem, int oldId)
        {
            if (thumbDir == null)
                return;

            string currDir = string.Format(@"{0}\{1}\{2}", thumbDir, thumbItem.ThumbFolder, oldId);
            if (!Directory.Exists(currDir))
                return;

            string[] files;
            try { files = Directory.GetFiles(currDir); }
            catch (Exception ex)
            {
                Logger.LogError("Error reading backup thumb directory '{0}' - {1}", currDir, ex.Message);
                return;
            }

            if (files.Length > 1)
            {
                using (ThumbGroup thumbGroup = new ThumbGroup(thumbItem))
                {
                    foreach (string file in files)
                    {
                        ThumbType thumbType;
                        if (ThumbGroup.IsThumbFile(file, out thumbType))
                        {
                            if (!thumbGroup.HasLocalThumb(thumbType))
                            {
                                thumbGroup.UpdateThumb(thumbType, file);
                                thumbGroup.SaveThumb(thumbType);
                            }
                        }
                        else if (ThumbGroup.IsManualFile(file) && string.IsNullOrEmpty(thumbGroup.ManualPath))
                        {
                            thumbGroup.ManualPath = file;
                            thumbGroup.SaveManual();
                        }
                    }
                }
            }
        }

        static Dictionary<string, Emulator> getEmulatorNames()
        {
            Dictionary<string, Emulator> emulatorNames = new Dictionary<string, Emulator>();
            foreach (Emulator emu in EmulatorsCore.Database.GetAll<Emulator>())
                if (!emulatorNames.ContainsKey(emu.Title))
                    emulatorNames.Add(emu.Title, emu);
            return emulatorNames;
        }

        static Dictionary<string, Game> getGamePaths()
        {
            Dictionary<string, Game> gamePaths = new Dictionary<string, Game>();
            foreach (Game game in EmulatorsCore.Database.GetAll<Game>())
                foreach (GameDisc disc in game.Discs)
                    gamePaths[disc.Path] = game;
            return gamePaths;
        }

        #endregion
    }
}
