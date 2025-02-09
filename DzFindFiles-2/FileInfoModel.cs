using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DzFindFiles_2
{
    class FileInfoModel
    {
        public string Name { get; set; }
        public string Folder { get; set; }
        public string Size { get; set; }
        public string Date { get; set; }

        public FileInfoModel(string name, string folder, string size, string data)
        {
            Name = name;
            Folder = folder;
            Size = size;
            Date = data;
        }

    }
}
