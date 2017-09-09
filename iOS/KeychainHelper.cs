using System;
using Foundation;
using Security;

namespace OMAPGMap.iOS
{
    public class KeychainHelper
    {
        private string ServiceName = "omahapgmap";

		public string ValueForKey(string key)
		{
			var record = ExistingRecordForKey(key);
			SecStatusCode resultCode;
			var match = SecKeyChain.QueryAsRecord(record, out resultCode);

			if (resultCode == SecStatusCode.Success)
				return NSString.FromData(match.ValueData, NSStringEncoding.UTF8);
			else
				return String.Empty;
		}

		public void SetValueForKey(string value, string key)
		{
			var record = ExistingRecordForKey(key);
			if (string.IsNullOrEmpty(value))
			{
				if (!string.IsNullOrEmpty(ValueForKey(key)))
					RemoveRecord(record);

				return;
			}

			// if the key already exists, remove it
            if (!string.IsNullOrEmpty(ValueForKey(key)))
				RemoveRecord(record);

			var result = SecKeyChain.Add(CreateRecordForNewKeyValue(key, value));
			if (result != SecStatusCode.Success)
			{
				throw new Exception(String.Format("Error adding record: {0}", result));
			}
		}

		private SecRecord CreateRecordForNewKeyValue(string key, string value)
		{
			return new SecRecord(SecKind.GenericPassword)
			{
				Account = key,
				Service = ServiceName,
				Label = key,
				ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
			};
		}

		private SecRecord ExistingRecordForKey(string key)
		{
			return new SecRecord(SecKind.GenericPassword)
			{
				Account = key,
				Service = ServiceName,
				Label = key,
			};
		}

		private bool RemoveRecord(SecRecord record)
		{
			var result = SecKeyChain.Remove(record);
			if (result != SecStatusCode.Success)
			{
				throw new Exception(String.Format("Error removing record: {0}", result));
			}

			return true;
		}
    }
}
