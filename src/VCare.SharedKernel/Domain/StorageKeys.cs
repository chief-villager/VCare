namespace VCare.SharedKernel.Domain
{
    public static class StorageKeys
    {
        /// <summary>The name the file was uploaded with. Keys are "{home}/{folder}/{id}_{name}".</summary>
        public static string DownloadName(string storageKey)
        {
            var last = storageKey[(storageKey.LastIndexOf('/') + 1)..];
            var underscore = last.IndexOf('_');
            return underscore >= 0 && underscore < last.Length - 1 ? last[(underscore + 1)..] : last;
        }
    }
}
