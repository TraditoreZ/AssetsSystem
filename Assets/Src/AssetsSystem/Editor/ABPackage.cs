

using System.Collections.Generic;

namespace TF.AssetEditor
{
    public class ABPackage
    {
        public string packageName;

        public HashSet<string> assets = new HashSet<string>();

        public double size_MB;

        public string[] options;
    }
}