    using GrinGlobal.Business;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Text;

    namespace GrinGlobal.WCFService
    {
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WCFService.svc or WCFService.svc.cs at the Solution Explorer and start debugging.
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class WCFService : IWCFService
        {
            public Stream Getdata(string dataview, string parameters, string limit)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;

                var JSONString = string.Empty;
                try
                {
                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");

                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        if (string.IsNullOrEmpty(limit)) limit = "0";
                        var dsResponse = sd.GetData(dataview, parameters, 0, int.Parse(limit), "");
                        if (dsResponse != null && dsResponse.Tables.Contains(dataview))
                        {
                            JSONString = JsonConvert.SerializeObject(dsResponse.Tables[dataview]);
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Status Login(Credential credential)
            {
                var status = new Status();
                try
                {
                    var dsValidateLogin = SecureData.TestLogin(false, credential.Username, credential.Password);

                    if (dsValidateLogin.Tables.Contains("validate_login") && dsValidateLogin.Tables["validate_login"].Rows.Count > 0)
                    {
                        status.CooperatorId = dsValidateLogin.Tables["validate_login"].Rows[0].Field<int>("cooperator_id");
                        status.Token = SecureData.Login(credential.Username, credential.Password);
                        if (string.IsNullOrEmpty(status.Token))
                        {
                            status.Success = false;
                            status.Error = "Error generating token";
                        }
                        else
                        {
                            status.Success = true;
                        }
                    }
                    else
                    {
                        status.Success = false;
                        status.Error = "Error validating credentials";
                    }
                }
                catch (Exception ex)
                {
                    status.Success = false;
                    status.Error = ex.Message;
                }

                return status;
            }

            public Stream CreateData(string tablename, string data)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;
                var JSONString = string.Empty;

                try
                {
                    var entity = JObject.Parse(data);
                    //var entity2 = JsonConvert.DeserializeObject(data);

                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");
                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        var dataset = sd.GetData("table_" + tablename, ":" + tablename.Replace("_", "") + "id=0", 0, 0, "");
                        var table = dataset.Tables["table_" + tablename];
                        var row = table.NewRow();

                        foreach (DataColumn c in table.Columns)
                        {
                            JToken prop = entity[c.ColumnName];
                            if (prop != null && prop.Type != JTokenType.Null && !prop.HasValues)
                                row.SetField(c, prop);
                        }
                        table.Rows.Add(row);

                        DataSet dsChanges = new DataSet();
                        dsChanges.Tables.Add(table.GetChanges());
                        var dsError = sd.SaveData(dsChanges, true, "");
                        if (dsError.Tables.Contains("table_" + tablename))
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK; //200
                            JSONString = JsonConvert.SerializeObject(dsError.Tables["table_" + tablename].Rows[0]["NewPrimaryKeyID"]);
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                            JSONString = JsonConvert.SerializeObject(dsError.Tables["ExceptionTable"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream ReadData(string tablename, string id)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;

                var JSONString = string.Empty;
                try
                {
                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");
                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        var dataset = sd.GetData("table_" + tablename, ":" + tablename.Replace("_", "") + "id=" + id, 0, 0, "");
                        var table = dataset.Tables["table_" + tablename];
                        if (dataset.Tables["table_" + tablename].Rows.Count > 0)
                        {
                            var row = table.Rows[0];
                            var entity = new Dictionary<string, object>();
                            foreach (DataColumn c in table.Columns)
                            {
                                entity.Add(c.ColumnName, row.Field<object>(c.ColumnName));
                            }
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK; //200
                            JSONString = JsonConvert.SerializeObject(entity);
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                            JSONString = JsonConvert.SerializeObject("Resource Not Found");
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream UpdateData(string tablename, string id, string data)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;
                var JSONString = string.Empty;

                try
                {
                    var entity = JObject.Parse(data);

                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");
                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        var dataset = sd.GetData("table_" + tablename, ":" + tablename.Replace("_", "") + "id=" + id, 0, 0, "");

                        if (dataset.Tables["table_" + tablename].Rows.Count > 0)
                        {
                            var table = dataset.Tables["table_" + tablename];
                            var row = table.Rows[0];

                            foreach (DataColumn c in table.Columns)
                            {
                                JToken prop = entity[c.ColumnName];
                                if (prop != null && !c.ReadOnly)
                                {
                                    if (prop.Type == JTokenType.Null)
                                    {
                                        row[c] = DBNull.Value;
                                    }
                                    else
                                    {
                                        row.SetField(c, prop);
                                    }
                                }
                            }

                            var dsChanges = new DataSet();
                            dsChanges.Tables.Add(table.GetChanges());
                            var dsError = sd.SaveData(dsChanges, true, "");

                            if (dsError.Tables.Contains("table_" + tablename))
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK; //200
                                JSONString = string.Empty;
                                /*if (dsError.Tables["table_" + tablename].Rows.Count > 0)
                                    JSONString = JsonConvert.SerializeObject(dsError.Tables["table_" + tablename].Rows[0]);
                                */
                            }
                            else
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                                JSONString = JsonConvert.SerializeObject(dsError.Tables["ExceptionTable"]);
                            }
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                            JSONString = JsonConvert.SerializeObject("Resource Not Found");
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream DeleteData(string tablename, string id)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;
                var JSONString = string.Empty;

                try
                {
                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");
                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        var dataset = sd.GetData("table_" + tablename, ":" + tablename + "id=" + id, 0, 0, "");

                        if (dataset.Tables["table_" + tablename].Rows.Count > 0)
                        {
                            var table = dataset.Tables["table_" + tablename];
                            var nr = table.Rows[0];
                            nr.Delete();

                            var dsError = sd.SaveData(dataset, true, "");
                            if (dsError.Tables.Contains("table_" + tablename))
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
                                JSONString = string.Empty;
                            }
                            else
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                                JSONString = JsonConvert.SerializeObject(dsError.Tables["ExceptionTable"]);
                            }
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                            JSONString = JsonConvert.SerializeObject("Resource Not Found");
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream SearchData(string tablename, string dataview, string query, string limit, string offset)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;

                var JSONString = string.Empty;
                try
                {
                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");

                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        if (string.IsNullOrEmpty(offset)) offset = "0";
                        if (string.IsNullOrEmpty(limit)) limit = "0";
                        //sd.Search(query, ignoreCase, andTermsTogether, indexList, resolverName, offset, limit, 0, 0, null, options, null, null)
                        DataSet ds = sd.Search(query, true, true, null, tablename, int.Parse(offset), int.Parse(limit), 0, 0, null, "", null, null);

                        if (ds.Tables["SearchResult"].Rows.Count > 0)
                        {
                            var ids = new List<string>();
                            foreach (DataRow row in ds.Tables["SearchResult"].Rows)
                            {
                                ids.Add(row.ItemArray[0].ToString());
                            }
                            if (string.IsNullOrEmpty(dataview))
                            {
                                var response = sd.GetData("table_" + tablename, ":" + tablename.Replace("_", "") + "id=" + string.Join(",", ids.ToArray()), int.Parse(offset), int.Parse(limit), "");
                                JSONString = JsonConvert.SerializeObject(response.Tables["table_" + tablename]);
                            }
                            else
                            {
                                var response = sd.GetData(dataview, ":" + tablename.Replace("_", "") + "id= " + string.Join(",", ids.ToArray()), int.Parse(offset), int.Parse(limit), "");
                                JSONString = JsonConvert.SerializeObject(response.Tables[dataview]);
                            }
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent; //204
                            return null;
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream SearchKeys(string tablename, string query, string limit, string offset)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;

                var JSONString = string.Empty;
                try
                {
                    var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");

                    using (SecureData sd = new SecureData(false, httpToken))
                    {
                        if (string.IsNullOrEmpty(offset)) offset = "0";
                        if (string.IsNullOrEmpty(limit)) limit = "0";
                        //sd.Search(query, ignoreCase, andTermsTogether, indexList, resolverName, offset, limit, 0, 0, null, options, null, null)
                        DataSet ds = sd.Search(query, true, true, null, tablename, int.Parse(offset), int.Parse(limit), 0, 0, null, "", null, null);

                        if (ds.Tables["SearchResult"].Rows.Count > 0)
                        {
                            var ids = new List<int>();
                            foreach (DataRow row in ds.Tables["SearchResult"].Rows)
                            {
                                ids.Add(row.Field<int>(0));
                            }
                            JSONString = JsonConvert.SerializeObject(ids);
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent; //204
                            return null;
                        }
                    }
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }

            public Stream printer(string printerId, string parameters)
            {
                var request = WebOperationContext.Current.IncomingRequest;
                var headers = request.Headers;
                var JSONString = string.Empty;

                try
                {
                    //var httpToken = headers["Authorization"].Trim().Replace("Bearer ", "");
                    //using (SecureData sd = new SecureData(false, httpToken))
                    //{
                    using (var connection = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["Oraculus"].ConnectionString))
                    {
                        connection.Open();

                        var dt = new DataTable();

                        var command = new System.Data.SqlClient.SqlCommand("select PrinterId, rtrim(PrinterName) as PrinterPath, PrinterResolution, rtrim(PrinterMethod) as PrinterMethod from printers where PrinterId = " + int.Parse(printerId), connection);
                        var da = new System.Data.SqlClient.SqlDataAdapter(command);
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            var dtr = dt.Rows[0];
                            var printerPath = dtr.Field<string>("PrinterPath");
                            var printerMethod = dtr.Field<string>("PrinterMethod");

                            if (string.IsNullOrEmpty(parameters))
                                parameters = "^XA\r\n^LH20,20  ^BY2,3\r\n^FO40,10  ^BXN,8,200 ^FD280810^FS\r\n^FO10,5   ^ADR,18^FD280810^FS\r\n^FO175,10 ^ADR,18^FDCOL Number^FS\r\n^FO200,5  ^ADR,18^FDCIP 460630^FS\r\n^LH20,110\r\n^FO130,10 ^ADR,18 ^FDCultvrname ^FS\r\n^FO100,10 ^ADR,18 ^FDCultvrname ^FS\r\n^FO70,10  ^ADR,18 ^FDTypeCrossName^FS\r\n^FO40,10  ^ADR,18 ^FD2017^FS\r\n^FO40,85  ^ADR,18 ^FDSPP ^FS\r\n^XZ";

                            JSONString = JsonConvert.SerializeObject(dt, Formatting.Indented);
                            if (printerMethod.Equals("Shared"))
                            {
                                var FILE_NAME = System.Web.Hosting.HostingEnvironment.MapPath("~/ZPLII.txt");
                                //var directory = System.Web.Hosting.HostingEnvironment.MapPath("~/uploads/logs");
                                //File.Create(FILE_NAME.Replace("ZPLII","ZPL2"));

                                File.WriteAllText(FILE_NAME, parameters);
                                File.Copy(FILE_NAME, printerPath);
                            }
                            else if (printerMethod.Equals("IP"))
                            {
                                const int port = 9100;

                                using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
                                {
                                    client.Connect(printerPath, port);

                                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(client.GetStream()))
                                    {
                                        writer.Write(parameters);
                                    }
                                }
                            }
                        }

                        da.Dispose();
                        connection.Close();
                    }
                    //}
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError; //500
                    JSONString = JsonConvert.SerializeObject(e.Message);
                }

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                return new MemoryStream(Encoding.UTF8.GetBytes(JSONString));
            }
        }
    }
