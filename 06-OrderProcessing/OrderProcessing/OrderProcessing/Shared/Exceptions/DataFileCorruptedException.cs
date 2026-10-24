using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public sealed class DataFileCorruptedException : Exception
    {
        public string FileName { get; }

        public DataFileCorruptedException(string fileName, Exception innerException)
            : base($"The JSON file '{fileName}' contains invalid JSON.", innerException)
        {
            FileName = fileName;
        }
    }
}
