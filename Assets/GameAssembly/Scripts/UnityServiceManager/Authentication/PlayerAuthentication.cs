using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameAssembly.Scripts.UnityServiceManager.Authentication
{
    public class PlayerAuthentication : MonoBehaviour
    {
        public UIDocument document;
        
        private const string FileName = "user_data.zip";
        private const string EntryName = "user_data.dat";
        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        
        private VisualElement _root;
        private TextField _username;
        private TextField _password;
        private Toggle _userToggle;
        private Button _loginBtn;
        private Button _registerBtn;
        private Label _output;
        private void OnEnable()
        {
            _root = document.rootVisualElement;
            _username = _root.Q<TextField>("Username");
            _password = _root.Q<TextField>("Password");
            _userToggle = _root.Q<Toggle>("UserToggle");
            _loginBtn = _root.Q<Button>("LoginButton");
            _registerBtn = _root.Q<Button>("RegisterButton");
            _output = _root.Q<Label>("Output");
        }

        private async void Awake()
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }
        }

        private void Start()
        {
            LoadUserData(out var rememberUser);
            _username.value = rememberUser.userName;
            _password.value = rememberUser.passWord;
            _userToggle.value = rememberUser.rememberMe;
            
            _userToggle.RegisterCallback<ChangeEvent<bool>>(OnUserToggleChanged);
            _loginBtn.RegisterCallback<ClickEvent>(OnLoginClick);
            _registerBtn.RegisterCallback<ClickEvent>(OnRegisterClick);
        }

        private async void OnRegisterClick(ClickEvent evt)
        {
            try
            {
                await SignUpWithUsernameAndPassword(_username.value, _password.value);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }
        }

        private async void OnLoginClick(ClickEvent evt)
        {
            try
            {
                await SignInWithUsernameAndPassword(_username.value, _password.value);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }
        }

        private void OnUserToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SaveUserData(new RememberUser
                {
                    rememberMe = evt.newValue,
                    userName = _username.value,
                    passWord = _password.value
                });
            }
            else
            {
                DeleteUserData();
            }
        }

        /// <summary>
        /// Lưu thông tin người dùng vào file ZIP trực tiếp
        /// </summary>
        /// <param name="userData">Dữ liệu người dùng cần lưu</param>
        /// <returns>True nếu lưu thành công, false nếu thất bại</returns>
        private static bool SaveUserData(RememberUser userData)
        {
            try
            {
                // Tạo hoặc ghi đè file ZIP
                using (var zipFileStream = new FileStream(SavePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var zipStream = new ZipOutputStream(zipFileStream))
                    {
                        // Thiết lập mức độ nén
                        zipStream.SetLevel(9);
                    
                        // Tạo entry mới cho file zip
                        var entry = new ZipEntry(EntryName)
                        {
                            DateTime = DateTime.Now
                        };

                        // Thêm entry vào file zip
                        zipStream.PutNextEntry(entry);
                    
                        // Serialize đối tượng trực tiếp vào ZipOutputStream
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(zipStream, userData);
                    
                        // Đóng entry
                        zipStream.CloseEntry();
                    }
                }
#if UNITY_EDITOR
                Debug.Log("Đã lưu dữ liệu người dùng thành công tại: " + SavePath);
#endif
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError("Lỗi khi lưu dữ liệu người dùng: " + e.Message);
#endif
                return false;
            }
        }

        /// <summary>
        /// Đọc thông tin người dùng từ file ZIP trực tiếp
        /// </summary>
        /// <param name="userData">RememberUser struct được trả về qua out parameter</param>
        /// <returns>True nếu đọc thành công, false nếu thất bại</returns>
        private static bool LoadUserData(out RememberUser userData)
        {
            userData = new RememberUser();
            
            // Kiểm tra xem file có tồn tại không
            if (!File.Exists(SavePath))
            {
#if UNITY_EDITOR
                Debug.Log("Không tìm thấy file dữ liệu người dùng.");
#endif
                return false;
            }
            
            try
            {
                using var zipFileStream = new FileStream(SavePath, FileMode.Open, FileAccess.Read);
                using var zipFile = new ZipFile(zipFileStream);
                // Tìm entry trong file zip
                var entry = zipFile.GetEntry(EntryName);
                    
                if (entry != null)
                {
                    // Lấy stream từ entry
                    using var zipStream = zipFile.GetInputStream(entry);
                    // Deserialize đối tượng trực tiếp từ stream của entry
                    var formatter = new BinaryFormatter();
                    userData = (RememberUser)formatter.Deserialize(zipStream);
#if UNITY_EDITOR   
                    Debug.Log("Đã đọc dữ liệu người dùng thành công.");
#endif
                    return true;
                }
#if UNITY_EDITOR
                Debug.LogWarning("Không tìm thấy dữ liệu trong file ZIP.");
#endif
                return false;
            }
            catch (SerializationException se)
            {
#if UNITY_EDITOR
                Debug.LogError("Lỗi định dạng dữ liệu: " + se.Message);
#endif
                return false;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError("Lỗi khi đọc dữ liệu người dùng: " + e.Message);
#endif
                return false;
            }
        }
        
        /// <summary>
        /// Xóa dữ liệu người dùng đã lưu
        /// </summary>
        /// <returns>True nếu xóa thành công hoặc file không tồn tại, false nếu có lỗi</returns>
        private static bool DeleteUserData()
        {
            if (!File.Exists(SavePath))
            {
                return true; // File không tồn tại, không cần xóa
            }
        
            try
            {
                File.Delete(SavePath);
#if UNITY_EDITOR
                Debug.Log("Đã xóa dữ liệu người dùng.");
#endif
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError("Lỗi khi xóa dữ liệu người dùng: " + e.Message);
#endif
                return false;
            }
        }
        
        [Serializable]
        private struct RememberUser
        {
            public bool rememberMe;
            public string userName;
            public string passWord;
        }

        private async Task SignInWithUsernameAndPassword(string username, string password)
        {
            _output.text = "";
            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
                if (_userToggle.value)
                {
                    SaveUserData(new RememberUser { rememberMe = true, userName = username, passWord = password });
                }
                else
                {
                    DeleteUserData();
                }
                _output.text = "Login successful";
            }
            catch (AuthenticationException)
            {
                //throw new ArgumentException(nameof(AuthenticationService), e);
            }
            catch (RequestFailedException)
            {
                _output.text = "Invalid username or password";
                //throw new ArgumentException(nameof(AuthenticationService), e);
            }
        }

        private async Task SignUpWithUsernameAndPassword(string username, string password)
        {
            _output.text = "";
            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                var userNameData = new Dictionary<string, object> { { "username", username } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(userNameData);
                if (_userToggle.value)
                {
                    SaveUserData(new RememberUser { rememberMe = true, userName = username, passWord = password });
                }
                else
                {
                    DeleteUserData();
                }
                _output.text = "Register successful!";
            }
            catch (AuthenticationException)
            {
                _output.text = "This account is already registered";
                //throw new ArgumentException(nameof(AuthenticationService), e);
            }
            catch (RequestFailedException)
            {
                _output.text = "Password does not match requirements. Insert at least 1 uppercase, 1 lowercase, 1 digit and 1 symbol. With minimum 8 characters and a maximum of 30";
                //throw new ArgumentException(nameof(AuthenticationService), e);
            }
        }
    }
}
