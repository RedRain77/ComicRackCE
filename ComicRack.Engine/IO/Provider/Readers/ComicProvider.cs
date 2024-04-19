using System;
using System.IO;
using System.Linq;
using cYo.Common.IO;
using cYo.Common.Reflection;

namespace cYo.Projects.ComicRack.Engine.IO.Provider.Readers
{
	public abstract class ComicProvider : ImageProvider, IInfoStorage
	{
		private static readonly string[] supportedTypes = new string[11]
		{
			"jpg",
			"jpeg",
			"jif",
			"jiff",
			"gif",
			"png",
			"tif",
			"tiff",
			"bmp",
			"djvu",
			"webp"
		};

		public bool UpdateEnabled => GetType().GetAttributes<FileFormatAttribute>().FirstOrDefault((FileFormatAttribute f) => f.Format.Supports(base.Source))?.EnableUpdate ?? false;

		protected bool DisableNtfs
		{
			get;
			set;
		}

		protected bool DisableSidecar
		{
			get;
			set;
		}

		public ComicInfo LoadInfo(InfoLoadingMethod method)
		{
			using (LockSource(readOnly: true))
			{
				ComicInfo comicInfo = (DisableNtfs ? null : NtfsInfoStorage.LoadInfo(base.Source));
				if (comicInfo == null && !DisableSidecar)
				{
					comicInfo = ComicInfo.LoadFromSidecar(base.Source);
				}
				if (comicInfo != null && method == InfoLoadingMethod.Fast)
				{
					return comicInfo;
				}
				ComicInfo comicInfo2 = OnLoadInfo();
				return comicInfo2 ?? comicInfo;
			}
		}

		public bool StoreInfo(ComicInfo comicInfo)
		{
			bool flag = false;
			using (LockSource(readOnly: false))
			{
				if (UpdateEnabled)
				{
					if (!OnStoreInfo(comicInfo))
					{
						return false;
					}
					flag = true;
				}
				if (!DisableNtfs)
				{
					flag |= NtfsInfoStorage.StoreInfo(base.Source, comicInfo);
				}
				return flag;
			}
		}

		protected virtual ComicInfo OnLoadInfo()
		{
			return null;
		}

		protected virtual bool OnStoreInfo(ComicInfo comicInfo)
		{
			return false;
		}

		protected virtual bool IsSupportedImage(string file)
        {
            if(ShouldIgnoreFile(file)) 
				return false;

            string fileExt = Path.GetExtension(FileUtility.MakeValidFilename(file));
            return supportedTypes.Any((string ext) => string.Equals(fileExt, "." + ext, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldIgnoreFile(string file)
        {
            string[] ignore = { ".DS_Store\\", "__MACOSX\\" };
            return ignore.Any(item => file.Contains(item));
        }
    }
}
