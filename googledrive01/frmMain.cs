using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Util;
using System.Web;
using Google.Apis.Authentication;
using System.Net;

namespace googledrive01
{
    public partial class frmMain : Form
    {
        String CLIENT_ID = "290928673113.apps.googleusercontent.com";
        String CLIENT_SECRET = "BNWwo6ZOKh7xCOq-Smw8UmGc";

        // Register the authenticator and create the service
        NativeApplicationClient provider = null;
        OAuth2Authenticator<NativeApplicationClient> auth = null;
        DriveService service = null;
        List<File> ls;
        File f;
        public frmMain()
        {
            InitializeComponent();
        }

        private void tsbtnLogin_Click(object sender, EventArgs e)
        {

            provider = new NativeApplicationClient(GoogleAuthenticationServer.Description, Properties.Settings.Default.CLIENT_ID, Properties.Settings.Default.CLIENT_SECRET);
            auth = new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization);
            service = new DriveService(auth);
            //FilesResource.ListRequest fl = service.Files.List();
            
            this.tsMessage.Text = "Login ok";

        }


        private IAuthorizationState GetAuthorization(NativeApplicationClient arg)
        {
            // Get the auth URL:
            IAuthorizationState state = new AuthorizationState(new[] { DriveService.Scopes.Drive.GetStringValue() });
            state.Callback = new Uri(NativeApplicationClient.OutOfBandCallbackUrl);
            IAuthorizationState result = null;
            string refreshToken = LoadRefreshToken();
            if (!String.IsNullOrEmpty(refreshToken))
            {
                state.RefreshToken = refreshToken;

                if (arg.RefreshToken(state))
                    return state;
            }
            frmAuthorizationCode ac = new frmAuthorizationCode();
            Uri authUri = arg.RequestUserAuthorization(state);

            // Request authorization from the user (by opening a browser window):
            Process.Start(authUri.ToString());

            if (ac.ShowDialog() == DialogResult.OK)
            {
                result = arg.ProcessUserAuthorization(ac.txtAC.Text, state);
                StoreRefreshToken(state);
            }
            return result;
        }

        private string LoadRefreshToken()
        {
            return Properties.Settings.Default.RefreshToken;
        }

        private void StoreRefreshToken(IAuthorizationState state)
        {
            Properties.Settings.Default.RefreshToken = state.RefreshToken;
            Properties.Settings.Default.Save();
        }

        private void tsbtnUpload_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                File body = new File();
                body.Title = "My document";
                body.Description = "A test document";
                body.MimeType = "text/plain";
                //body.MimeType = "application/rtf";
               // body.MimeType = "text/html";

                //byte[] byteArray = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                //System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);

                
                    //FilesResource.InsertMediaUpload request = service.Files.Insert(body, stream, "text/plain");

                //request.Upload();

                //File file = request.ResponseBody;
                string s = System.IO.File.ReadAllText(openFileDialog.FileName);
                File ff= Utils.InsertResource(service, auth, body.Title, body.Description, body.MimeType, s);
                
                this.tsMessage.Text = "File id: " + ff.Id.ToString();

            }
        }

        private void tsList_Click(object sender, EventArgs e)
        {
            ls = retrieveAllFiles(service);

        }



        /// <summary>
        /// Retrieve a list of File resources.
        /// </summary>
        /// <param name="service">Drive API service instance.</param>
        /// <returns>List of File resources.</returns>

        public static List<File> retrieveAllFiles(DriveService service)
        {
            List<File> result = new List<File>();
            FilesResource.ListRequest request = service.Files.List();

            do
            {
                try
                {
                    FileList files = request.Fetch();

                    result.AddRange(files.Items);
                    request.PageToken = files.NextPageToken;
                }
                catch (Exception e)
                {
                    //tsMessage.Text="An error occurred: " + e.Message;
                    request.PageToken = null;
                }
            } while (!String.IsNullOrEmpty(request.PageToken));
            return result;
        }



        public static File retrieveFiles(DriveService service, string filename, List<File> ls)
        {
            File result = new File();
            

            
            try
            {   if (ls== null)
                    ls = retrieveAllFiles(service);
                foreach (File f in ls)
                {
                    if (f.Title == filename)
                        return result = f;
                        
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("An error occurred: " + e.Message);
                    
            }
            
            return result;
        }

        private void tsOpen_Click(object sender, EventArgs e)
        {
             f = retrieveFiles(service, "My document", ls);
            
            //File f1 = service.Files.Get(f.id).Fetch();
            //System.IO.Stream ss = DownloadFile(auth, f);

            string s = Utils.DownloadFile(auth, f.DownloadUrl);
            
            txtText.Text = s;
            
        }

        /// <summary>
        /// Download a file and return a string with its content.
        /// </summary>
        /// <param name="authenticator">
        /// Authenticator responsible for creating authorized web requests.
        /// </param>
        /// <param name="file">Drive File instance.</param>
        /// <returns>File's content if successful, null otherwise.</returns>
        public static System.IO.Stream DownloadFile(
            IAuthenticator authenticator, File file)
        {
            if (!String.IsNullOrEmpty(file.DownloadUrl))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                        new Uri(file.DownloadUrl));
                    authenticator.ApplyAuthenticationToRequest(request);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return response.GetResponseStream();
                    }
                    else
                    {
                        Console.WriteLine(
                            "An error occurred: " + response.StatusDescription);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                // The file doesn't have any content stored on Drive.
                return null;
            }
        }

        private void tsUpdate_Click(object sender, EventArgs e)
        {
            if (f == null) 
                f = retrieveFiles(service, "My document", ls); ;
            Utils.UpdateResource(service, auth, f.Id, f.Title, f.Description, f.MimeType, txtText.Text, true);
        }

        private void frmMain_Leave(object sender, EventArgs e)
        {
            
        }

        private void frmMain_Deactivate(object sender, EventArgs e)
        {
            this.Text = DateTime.Now.ToString();
        }
    }
}
