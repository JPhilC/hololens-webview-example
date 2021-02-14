using UnityEngine;
using System;
using System.IO;
using System.Linq;
using Vuplex.WebView;
using System.Threading.Tasks;

class HololensWebViewDemo : MonoBehaviour {

    WebViewPrefab _webViewPrefab;
    private bool _topicLoaded;


    void Start() {

        // Create a 0.6 x 0.3 instance of the prefab.
        _webViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.3f);
        _webViewPrefab.transform.parent = transform;
        _webViewPrefab.InitialResolution = 1300f;
        _webViewPrefab.transform.localPosition = new Vector3(0, 0.2f, 1);
        _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
        _webViewPrefab.Initialized += (sender, e) => {
            _webViewPrefab.WebView.LoadUrl("https://congility.com");
        };

    }
    void Update()
    {
        
        if (!_topicLoaded)
        {
            System.Diagnostics.Debug.WriteLine("Reading content");
            ContentData contentData = new ContentData();
            Task.WaitAll(Task.Run(async () =>
            {
                contentData = await ContentService.LoadContent();
            }));

            if (contentData.Files.Any())
            {
                System.Diagnostics.Debug.WriteLine("Content found");
            }
            ContentFile firstFile = contentData.Files.FirstOrDefault();
            ContentTopic firstTopic = firstFile?.Topics.FirstOrDefault();
            if (firstTopic != null)
            {
                System.Diagnostics.Debug.WriteLine("Unzipping content ...");
                string extractedTo = string.Empty;
                Task.WaitAll(Task.Run(async () =>
                {
                    extractedTo = await ContentService.UnzipContent(firstFile.Filename);

                }));
                if (extractedTo != string.Empty)
                {
                    string url = $"ms-appdata:///local/Content/{firstTopic.LandingPage}";
                    //string url = Path.Combine(extractedTo, firstTopic.LandingPage);

                    System.Diagnostics.Debug.WriteLine($"Loading Url: {url}");
                    _webViewPrefab.WebView.LoadUrl(url);
                    System.Diagnostics.Debug.WriteLine($"Url loaded.");
                    _topicLoaded = true;
                }
            }

        }

    }


    
    
}
