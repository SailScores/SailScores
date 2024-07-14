namespace SailScores.Web.IndexNow;


//POST /IndexNow HTTP/1.1
//Content-Type: application/json; charset=utf-8
//Host: api.indexnow.org
//{
//  "host": "www.example.org",
//  "key": "1654280df6a54884a8f288d0d98a33ae",
//  "keyLocation": "https://www.example.org/1654280df6a54884a8f288d0d98a33ae.txt",
//  "urlList": [
//      "https://www.example.org/url1",
//      "https://www.example.org/folder/url2",
//      "https://www.example.org/url3"
//      ]
//}
public class Submission
{
    public string Host { get; set; }
    public string Key { get; set; }
    public string KeyLocation { get; set; }
    public List<string> UrlList { get; set; }
}
