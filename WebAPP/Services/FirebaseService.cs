using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System;

public class FirebaseService
{
    
        public FirebaseService(IConfiguration configuration)
        {
            // Đọc tệp JSON chứa thông tin tài khoản dịch vụ Firebase
            var pathToCredentials = configuration["Firebase:CredentialsPath"];
            if (string.IsNullOrEmpty(pathToCredentials))
            {
                throw new InvalidOperationException("Firebase credentials file path is not configured.");
            }

            // Khởi tạo Firebase Admin SDK
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(pathToCredentials)
            });
        }


}
