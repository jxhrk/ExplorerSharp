using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerSharp
{
    internal class Utils
    {
        public static string GetDirDisplayName(string dir)
        {
            if (Directory.GetParent(dir) == null)
            {
                List<DriveInfo> driveInfos = DriveInfo.GetDrives().ToList();
                DriveInfo drive = driveInfos.Find(o => o.Name == dir);
                string defaultName = ExplorerSharp.Resources.DriveDefaultName;
                string diskName = drive.VolumeLabel == "" ? defaultName : drive.VolumeLabel;

                string diskLetter = dir.Split(':')[0];

                return $"{diskName} ({diskLetter}:)";
            }
            else
            {
                return Path.GetFileName(dir);
            }
        }
    }
}
