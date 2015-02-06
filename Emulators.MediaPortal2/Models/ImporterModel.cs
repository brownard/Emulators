using Emulators.Import;
using Emulators.MediaPortal2.View_Models;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models
{
    public class ImporterModel : IWorkflowModel
    {
        object syncRoot = new object();
        Importer importer;
        Dictionary<int, RomMatchViewModel> itemsDictionary;
        ItemsList items;

        AbstractProperty _statusProperty;
        AbstractProperty _progressProperty;

        public ImporterModel()
        {
            _statusProperty = new WProperty(typeof(string), null);
            _progressProperty = new WProperty(typeof(int), 0);
        }

        void initImporter()
        {
            lock (syncRoot)
            {
                if (importer != null)
                    return;

                itemsDictionary = new Dictionary<int, RomMatchViewModel>();
                items = new ItemsList();
                importer = new Importer();
                importer.ImportStatusChanged += importer_ImportStatusChanged;
                importer.ProgressChanged += importer_ProgressChanged;
                importer.RomStatusChanged += importer_RomStatusChanged;
                importer.Start();
            }
        }

        void importer_RomStatusChanged(object sender, RomStatusEventArgs e)
        {
            updateItem(e.RomMatch);
        }

        void importer_ProgressChanged(object sender, ImportProgressEventArgs e)
        {
            Progress = e.Percent;
            Status = e.Total > 0 ? string.Format("{0}/{1} - {2}", e.Current, e.Total, e.Description) : e.Description;
        }

        void importer_ImportStatusChanged(object sender, ImportStatusEventArgs e)
        {
            if (e.Action == ImportAction.PendingFilesAdded)
                rebuildItemsList(e.NewItems);
        }

        void updateItem(RomMatch romMatch)
        {
            lock (syncRoot)
            {
                RomMatchViewModel model;
                if (itemsDictionary.TryGetValue(romMatch.ID, out model))
                {
                    model.Update();
                    return;
                }
                model = new RomMatchViewModel(romMatch);
                itemsDictionary[romMatch.ID] = model;
                items.Add(model);
            }
            items.FireChange();
        }

        void rebuildItemsList(List<RomMatch> newItems)
        {
            lock (syncRoot)
            {
                itemsDictionary.Clear();
                items.Clear();
                foreach (RomMatch romMatch in newItems)
                {
                    RomMatchViewModel model = new RomMatchViewModel(romMatch);
                    itemsDictionary[romMatch.ID] = model;
                    items.Add(model);
                }
            }
            items.FireChange();
        }

        public AbstractProperty StatusProperty { get { return _statusProperty; } }
        public string Status
        {
            get { return (string)_statusProperty.GetValue(); }
            set { _statusProperty.SetValue(value); }
        }

        public AbstractProperty ProgressProperty { get { return _progressProperty; } }
        public int Progress
        {
            get { return (int)_progressProperty.GetValue(); }
            set { _progressProperty.SetValue(value); }
        }

        public ItemsList Items
        {
            get { return items; }
        }

        #region IWorkflow

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {
            
        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {
            
        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            initImporter();
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            
        }

        public Guid ModelId
        {
            get { return Guids.ImporterModel; }
        }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {
            
        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {
            
        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }

        #endregion
    }
}
