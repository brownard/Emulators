using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class TextInputModel
    {
        protected AbstractProperty _headerProperty;
        protected AbstractProperty _textProperty;
        protected AbstractProperty _buttonTextProperty;

        Action<string> textAcceptedHandler;

        public TextInputModel()
        {
            _headerProperty = new WProperty(typeof(string), null);
            _textProperty = new WProperty(typeof(string), null);
            _buttonTextProperty = new WProperty(typeof(string), null);
        }

        public static TextInputModel Instance()
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            return (TextInputModel)workflowManager.GetModel(Guids.TextInputDialogModel);
        }

        public void ShowDialog(string header, string text, string buttonText, Action<string> textAcceptedHandler)
        {
            Header = header;
            Text = text;
            ButtonText = buttonText;
            this.textAcceptedHandler = textAcceptedHandler;
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog(Consts.DIALOG_TEXT_INPUT);
        }

        public void TextAccepted()
        {
            if (textAcceptedHandler != null)
            {
                textAcceptedHandler(Text);
                textAcceptedHandler = null;
            }
        }

        public AbstractProperty HeaderProperty { get { return _headerProperty; } }
        public string Header
        {
            get { return (string)_headerProperty.GetValue(); }
            set { _headerProperty.SetValue(value); }
        }

        public AbstractProperty TextProperty { get { return _textProperty; } }
        public string Text
        {
            get { return (string)_textProperty.GetValue(); }
            set { _textProperty.SetValue(value); }
        }

        public AbstractProperty ButtonTextProperty { get { return _buttonTextProperty; } }
        public string ButtonText
        {
            get { return (string)_buttonTextProperty.GetValue(); }
            set { _buttonTextProperty.SetValue(value); }
        }
    }
}
