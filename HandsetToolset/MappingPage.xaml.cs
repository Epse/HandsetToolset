using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HandsetToolset
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MappingPage : Page
    {
        private readonly CommandProcessor _processor = CommandProcessor.Current;
        private readonly ObservableCollection<string> _buttons;

        public MappingPage()
        {
            this.InitializeComponent();

            _buttons = new ObservableCollection<string>();
            foreach (var btn in Enum.GetNames(typeof(VirtualKey)))
            {
                _buttons.Add(btn);
            }
        }
    }
}
