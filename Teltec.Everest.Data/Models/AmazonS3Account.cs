/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

namespace Teltec.Everest.Data.Models
{
	public class AmazonS3Account : StorageAccount
    {
		public const int DisplayNameMaxLen = 16;

		public override EStorageAccountType Type
		{
			get { return EStorageAccountType.AmazonS3;  }
		}

        // http://docs.aws.amazon.com/IAM/latest/APIReference/API_AccessKey.html
        public const int AccessKeyIdMinLen = 16;
        public const int AccessKeyIdMaxLen = 32;
        private String _AccessKey;
		public virtual String AccessKey
        {
            get { return _AccessKey; }
            set { SetField(ref _AccessKey, value); }
        }

        private String _SecretKey;
		public virtual String SecretKey
        {
            get { return _SecretKey; }
            set { SetField(ref _SecretKey, value); }
        }

        // http://docs.aws.amazon.com/AmazonS3/latest/dev/BucketRestrictions.html
        public const int BucketNameMinLen = 3;
        public const int BucketNameMaxLen = 63;
        private String _BucketName;
		public virtual String BucketName
        {
            get { return _BucketName; }
            set { SetField(ref _BucketName, value); }
        }

		//private IList<BackupPlan> _BackupPlans = new List<BackupPlan>();
		//public virtual IList<BackupPlan> BackupPlans
		//{
		//	get { return _BackupPlans; }
		//	set { SetField(ref _BackupPlans, value); }
		//}
    }
}
