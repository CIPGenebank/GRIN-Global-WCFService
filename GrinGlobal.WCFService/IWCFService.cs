using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace GrinGlobal.WCFService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWCFService" in both code and config file together.
    [ServiceContract]
    public interface IWCFService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "printer/{printerId}")]
        Stream printer(string printerId, string parameters);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "getdata/{dataview}?parameters={parameters}")]
        Stream getdata(string dataview, string parameters);

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        Status Login(Credential credential);

        #region REST_table_dataview

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "rest/{tablename}", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        Stream CreateData(string tablename, string data);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "rest/{tablename}/{id}", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ReadData(string tablename, string id);

        [OperationContract]
        [WebInvoke(Method = "PUT", UriTemplate = "rest/{tablename}/{id}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream UpdateData(string tablename, string id, string data);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "rest/{tablename}/{id}", ResponseFormat = WebMessageFormat.Json)]
        Stream DeleteData(string tablename, string id);

        #endregion

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "search/{tablename}?dataview={dataview}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream SearchData(string tablename, string dataview, string query);
    }

    public class Status
    {
        public bool Success { set; get; }
        public string Error { set; get; }
        public string Token { set; get; }
        public int CooperatorId { set; get; }

        public Status() { CooperatorId = -1; }
    }

    public class Credential
    {
        public string Username { set; get; }
        public string Password { set; get; }
    }
}
