using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.ServiceModel.Web;

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
        [WebInvoke(Method = "GET", UriTemplate = "getdata/{dataview}?parameters={parameters}&limit={limit}")]
        Stream Getdata(string dataview, string parameters, string limit);

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
        [WebInvoke(Method = "POST", UriTemplate = "search/{tablename}?dataview={dataview}&limit={limit}&offset={offset}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream SearchData(string tablename, string dataview, string query, string limit, string offset);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "searchkeys/{tablename}?limit={limit}&offset={offset}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream SearchKeys(string tablename, string query, string limit, string offset);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "print?printURI={printURI}&printConnectionType={printConnectionType}")]
        Stream Print(string printURI, string printConnectionType, string zpl);
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
