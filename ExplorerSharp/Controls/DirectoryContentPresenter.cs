using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerSharp.Controls
{
    public interface IDirectoryContentPresenter
    {
        public void SetContent(List<FileInfo> files) { }
        public void SelectFile(int index) { }
        public void SelectFile(string fileName) { }
    }
}
