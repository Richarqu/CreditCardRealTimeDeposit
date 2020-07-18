using Newtonsoft.Json;
using RestSharp;
using Sterling.MSSQL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CreditCardRealTimeDeposit
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public   static async Task Main(string[] args)
        {
            try
            {
                #region read Card details of from cardfile
                //string body = string.Empty;
                //using (StreamReader reader = new StreamReader("./SampleFile/new_th."))
                //{
                //    body = reader.ReadToEnd();
                //}
                //string sep = "\n";
                //string[] splitContent = body.Split(sep.ToCharArray());
                //List<CardDetails> cardDetailList = new List<CardDetails>();
                //foreach (var card in splitContent)
                //{
                //    var cardArray = card.Split('|');
                //    CardDetails cardDetails = new CardDetails();
                //    cardDetails.AccountNumber = cardArray[0].Trim().Substring(0, 10);
                //    cardDetails.AccountCurrencyCode = cardArray[0].Trim().Substring(cardArray[0].Length - 3); ;
                //    cardDetails.Pan = cardArray[1].Trim();
                //    cardDetails.CardCurrencyCode = cardArray[2].Trim();
                //    cardDetails.Cardtype = cardArray[3].Trim();
                //    cardDetails.DateIssued = cardArray[4].Trim();
                //    cardDetails.ExpiryDate = cardArray[5].Trim();
                //    cardDetails.DateActivated = cardArray[6].Trim();
                //    //cardDetails.dateactivated = cardArray[7].Trim();
                //    cardDetails.Limit = cardArray[8].Trim();
                //    cardDetails.LastPaymentMade = cardArray[9].Trim();
                //    cardDetails.Status = cardArray[12].Trim();
                //    cardDetailList.Add(cardDetails);
                //}
                //// cardDetails.limit = cardArray[0];
                #endregion
                //var res = CallFIMIService();
                await CallFIMIService();
            }
            catch (Exception ex)
            {
                Logger.Error("CallFIMIService Exception:"+ ex.Message +"StackTrace:"+ex.StackTrace);
            }
           
        }

        static string[] getProductInfo(string code)
        {
            var prod = new string[2];

            var sql = "SELECT Misc,CurrencyCode FROM [Product] WHERE Misc = 'Visa Credit' AND CODE = @code UNION SELECT Misc,CurrencyCode FROM [Product] WHERE Misc = 'Payment Files' AND CODE = @code order by Misc desc";
            Connect cn = new Connect("cardsMssqlconn");
            cn.Persist = true;
            cn.SetSQL(sql, 5000);
            cn.AddParam("@code", code);
            DataSet ds = cn.Select();
            cn.CloseAll();

            bool hasRow = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRow)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                DataRow dr2 = ds.Tables[0].Rows[1];
                prod[0] = dr[1].ToString();
                prod[1] = dr2[1].ToString();
            }

            return prod;
        }
        public async static Task<bool> CallFIMIService()
        {
            Logger.Info("CallFIMIService Called");
           // EACBSServiceReference.banksSoapClient banks = new EACBSServiceReference.banksSoapClient();
            //var nuban = "0066501917";
            var nuban = String.Empty;
            var accountCurrencyCode = String.Empty;
            var cardCurrencyCode = String.Empty;
            var baseUrl = ConfigurationManager.AppSettings["BaseUrl"].ToString();
            var PostParam = String.Empty;
            var connectionString = ConfigurationManager.AppSettings["mssqlconn"].ToString();
            var cardConnectionString = ConfigurationManager.AppSettings["cardDetailsconn"].ToString();
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            SqlConnection cardCon = new SqlConnection(cardConnectionString);
            
            try
            {
              
                #region Pending Credit Card Transactions
                //Get all pending deposits
                //Create connection and open it.

                //create command object to pass the connection and other information
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                //set command type as stored procedure
                cmd.CommandType = CommandType.StoredProcedure;
                //pass the stored procedure name
                cmd.CommandText = "SelectPendingRecords";
                //Execute the query
                SqlDataReader res = cmd.ExecuteReader();
                // int res = cmd.ExecuteNonQuery();
                List<PendingTransaction> pendingTransactionList = new List<PendingTransaction>();

                if (res.HasRows)
                {
                    while (res.Read())
                    {
                        PendingTransaction pendingTransaction = new PendingTransaction();
                        pendingTransaction.accNum = (string)res["AccountNumber"];
                        pendingTransaction.amount = res["TransactionAmount"].ToString();
                        pendingTransaction.currency = (string)res["Currency"];
                        pendingTransaction.transId = Guid.Parse(res["TransId"].ToString());
                        pendingTransaction.TransactionDate =DateTime.Parse(res["TransactionDate"].ToString());
                        pendingTransaction.TransRef = (string)res["TransRef"];
                        pendingTransactionList.Add(pendingTransaction);
                    }
                    // res.Close();
                    // forea
                    foreach (var pendingDeposit in pendingTransactionList)
                    {
                        con.Close();
                        try
                        {
                            cardCon.Close();
                            Logger.Info("CallFIMIService cardCon.Close()");
                        }
                        catch(Exception ex)
                        {
                            Logger.Error("CallFIMIService cardCon.Close() "+ ex.Message);
                        }
                       
                        cardCon.Open();
                        accountCurrencyCode = pendingDeposit.currency;
                        var nubanToCredit = pendingDeposit.accNum;
                        var currCode = string.Empty;
                        #region Get Card info of corresponding deposit
                        //Create connection and open it.
                        //create command object to pass the connection and other information
                        SqlCommand cardCmd = new SqlCommand();
                        cardCmd.Connection = cardCon;
                        cardCmd.CommandType = CommandType.StoredProcedure;
                        cardCmd.CommandText = "GetCardDetails";
                        cardCmd.Parameters.Add(new SqlParameter("@accountNumber", SqlDbType.VarChar)).Value = nubanToCredit;
                        SqlDataReader cardRes = cardCmd.ExecuteReader();
                        if (cardRes.HasRows)
                        {
                            List<CardDetails> cardDetailsList = new List<CardDetails>();
                            //  Response.Write("Data Inserted Successfully");
                            while (cardRes.Read())
                            {

                                CardDetails cardDetails = new CardDetails();
                                var cardExpireDayMonthArray = cardRes["ExpiryDate"].ToString().Split('/');
                                var cardExpireDayMonth = cardExpireDayMonthArray[2] + cardExpireDayMonthArray[1];
                                cardDetails.AccountNumber = (string)cardRes["AccountNumber"];
                                cardDetails.Pan = cardRes["Pan"].ToString();
                                cardDetails.ExpiryDate = cardExpireDayMonth;
                                //  cardDetails.TransId = (Guid)cardRes["TransId"];
                                var code = cardDetails.Pan.Substring(0, 6);
                               var productDetails= getProductInfo(code);
                                if (productDetails.Length>1)
                                {
                                    cardDetails.CardCurrencyCode = productDetails[1];
                                    currCode = productDetails[0];
                                }
                                else
                                {
                                    break;
                                }

                                //if (cardDetails.Pan.Substring(0, 6).Contains(ConfigurationManager.AppSettings["CreditCardCode"].ToString()))
                                //{
                                //    cardDetails.CardCurrencyCode = productDetails[1];
                                //    currCode = productDetails[0];
                                //}
                                //else
                                //{
                                //    cardDetails.CardCurrencyCode = "NGN";
                                //    currCode = ConfigurationManager.AppSettings["DefaultNairaCode"].ToString();
                                //}
                                cardDetailsList.Add(cardDetails);
                            }
                                var matchedCard = cardDetailsList.Where(x => x.CardCurrencyCode == accountCurrencyCode).FirstOrDefault();
                            if (matchedCard.Pan != null)
                            {

                                PosRequestModel posRequestModel = new PosRequestModel();
                                posRequestModel.PAN = matchedCard.Pan;
                                posRequestModel.CardExpiry = matchedCard.ExpiryDate;
                                posRequestModel.Amount = pendingDeposit.amount;
                                posRequestModel.Currency = currCode;
                                posRequestModel.IsDebit =false;
                                PostParam = JsonConvert.SerializeObject(posRequestModel);
                                var client = new RestClient(baseUrl + "POSDeposit");
                                //var clientCode = ConfigurationManager.AppSettings["ClientCode"].ToString();
                                var request = new RestRequest(Method.POST);
                                //request.AddHeader("clientCode", clientCode);
                                Logger.Info("CallFIMIService FIMI POS Deposit Request:"+ PostParam);
                                request.AddParameter("application/json; charset=utf-8", PostParam, ParameterType.RequestBody);
                                SqlConnection cardUpdateCmdCon = new SqlConnection(connectionString);
                                try
                                {
                                    cardCon.Close();
                                    IRestResponse response = await client.ExecuteTaskAsync(request);
                                    Logger.Info("CallFIMIService FIMI POS Deposit Request"+ PostParam+". Response:" + response.Content);
                                    var DeserializedResonse = JsonConvert.DeserializeObject<PosRequestResponse.Envelope>(response.Content);
                                    var theResponse = response.Content;
                                   
                                    if (response.IsSuccessful)
                                    {
                                        if(DeserializedResonse.Body != null)
                                        {
                                            if (DeserializedResonse.Body.POSRequestRp != null)
                                            {
                                                if (DeserializedResonse.Body.POSRequestRp.Response != null)
                                                {
                                                    if (DeserializedResonse.Body.POSRequestRp.Response != null)
                                                    {
                                                        if (DeserializedResonse.Body.POSRequestRp.Response.AuthRespCode == "1")
                                                        {
                                                                cardUpdateCmdCon.Open();
                                                                SqlCommand cardUpdateCmd = new SqlCommand();
                                                                cardUpdateCmd.Connection = cardUpdateCmdCon;
                                                                cardUpdateCmd.CommandType = CommandType.StoredProcedure;
                                                                cardUpdateCmd.CommandText = "UpdatePendingRecord";
                                                                cardUpdateCmd.Parameters.Add(new SqlParameter("@transId", SqlDbType.UniqueIdentifier)).Value = pendingDeposit.transId;
                                                                SqlDataReader cardUpdateRes = cardUpdateCmd.ExecuteReader();

                                                                #region insert into exist EMP/FIMI transaction table
                                                                InsertTransactionModel insertTransactionMode = new InsertTransactionModel();
                                                                //var accountFullInfo = banks.getAccountFullInfo(matchedCard.AccountNumber);
                                                                // var CustName = accountFullInfo.Tables[0].Rows[0]["CUS_SHO_NAME"].ToString();
                                                                var accountFullInfo = await FioranoGetAccountFullInfo(matchedCard.AccountNumber);
                                                                var CustName = accountFullInfo.BankAccountFullInfo.CUS_SHO_NAME;
                                                                insertTransactionMode.UserId = "Administrator";
                                                                insertTransactionMode.PaymentDate = pendingDeposit.TransactionDate;
                                                                insertTransactionMode.CardAccountNumber = matchedCard.AccountNumber;
                                                                insertTransactionMode.CardHolderEmbossingName = CustName;
                                                                insertTransactionMode.Reference = pendingDeposit.TransRef;
                                                            //   insertTransactionMode.CardholderBillingCurrency = matchedCard.AccountCurrencyCode;
                                                                insertTransactionMode.CardholderBillingCurrency = currCode;
                                                                char pad = '0';
                                                                var pasddedAmount = pendingDeposit.amount.PadLeft(12, pad);
                                                                insertTransactionMode.CardholderBillingAmount = pasddedAmount;
                                                                insertTransactionMode.ReversalIndicator = "N";
                                                                insertTransactionMode.SupplementaryCardNumber = "0000000000000000000000000";
                                                                insertTransactionMode.ReasonCode = "PYMT";
                                                                insertTransactionMode.Filler = "00000000000000000000000000000000000000000000000000000000";
                                                                insertTransactionMode.PaymentIndicator = "3";
                                                                insertTransactionMode.BillingAmount = Convert.ToDecimal(pendingDeposit.amount);
                                                                insertTransactionMode.LogDate = DateTime.Now;
                                                                insertTransactionMode.FailedStatus = 0;
                                                                insertTransactionMode.FileName = "FIMI-Instant_Credit" + matchedCard.AccountNumber;
                                                                insertTransactionMode.Acknowledge = false;
                                                                await Insert(insertTransactionMode);
                                                                #endregion
                                                        }
                                                        else
                                                        {
                                                            Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                        }
                                    }
                                     
                                    else
                                    {
                                        Logger.Info("CallFIMIService FIMI POS Deposit failed:" + nubanToCredit);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Logger.Error("CallFIMIService Exception:"+ex.Message + "Account Number:"+ matchedCard.AccountNumber + ".StackTrace:" + ex.StackTrace);
                                 //   cardUpdateCmdCon.Close();
                                }
                                finally
                                {
                                    cardUpdateCmdCon.Close();
                                }
                            }
                            else
                            {
                                Logger.Info("CallFIMIService Unable to get card matched PAN:" + nubanToCredit);
                            }
                        }
                        else
                        {
                            Logger.Info("CallFIMIService Unable to get card details:" + nubanToCredit);
                        }
                    }
                    #endregion
                }
                else
                {
                    Logger.Info("CallFIMIService no pending records found");
                }
                #endregion
            }
            catch (Exception ex)
            {
                //silently
                Logger.Error("CallFIMIService Exception:" + ex.Message  + ".StackTrace:" + ex.StackTrace);
            }
            return false;
        }

        public async static Task<int> Insert(InsertTransactionModel insertTransactionModel)
        {
            var request = JsonConvert.SerializeObject(insertTransactionModel);
            var connectionString = ConfigurationManager.AppSettings["cardsMssqlconn"].ToString();
            SqlConnection con = new SqlConnection(connectionString);
            int ID = 0;
            con.Open();
            try
            {
                SqlCommand cn;
                SqlDataAdapter adapter = new SqlDataAdapter();
                var req = JsonConvert.SerializeObject(insertTransactionModel);
                Logger.Info("CallFIMIService Insert Begins. request:" + req);
                string sql = "INSERT INTO FailedPayment (UserId, PaymentDate, CardAccountNumber, CardHolderEmbossingName, Reference, CardholderBillingCurrency, CardholderBillingAmount, ReversalIndicator, SupplementaryCardNumber, ReasonCode, PaymentIndicator, Filler, billingAmount, LogDate, FailedStatus, FileName, IsDeleted, Acknowledge) VALUES ( @UserId, @PaymentDate, @CardAccountNumber, @CardHolderEmbossingName, @Reference, @CardholderBillingCurrency, @CardholderBillingAmount, @ReversalIndicator, @SupplementaryCardNumber, @ReasonCode, @PaymentIndicator, @Filler, @billingAmount, @LogDate, @FailedStatus, @FileName, @IsDeleted, @Acknowledge) ";
                cn = new SqlCommand(sql, con);
                cn.Parameters.AddWithValue("@UserId", insertTransactionModel.UserId);
                cn.Parameters.AddWithValue("@PaymentDate", insertTransactionModel.PaymentDate);
                cn.Parameters.AddWithValue("@CardAccountNumber", insertTransactionModel.CardAccountNumber);
                cn.Parameters.AddWithValue("@CardHolderEmbossingName", insertTransactionModel.CardHolderEmbossingName);
                cn.Parameters.AddWithValue("@Reference", insertTransactionModel.Reference);
                cn.Parameters.AddWithValue("@CardholderBillingCurrency", insertTransactionModel.CardholderBillingCurrency);
                cn.Parameters.AddWithValue("@CardholderBillingAmount", insertTransactionModel.CardholderBillingAmount);
                cn.Parameters.AddWithValue("@ReversalIndicator", insertTransactionModel.ReversalIndicator);
                cn.Parameters.AddWithValue("@SupplementaryCardNumber", insertTransactionModel.SupplementaryCardNumber);
                cn.Parameters.AddWithValue("@ReasonCode", insertTransactionModel.ReasonCode);
                cn.Parameters.AddWithValue("@PaymentIndicator", insertTransactionModel.PaymentIndicator);
                cn.Parameters.AddWithValue("@Filler", insertTransactionModel.Filler);
                cn.Parameters.AddWithValue("@billingAmount", insertTransactionModel.BillingAmount);
                cn.Parameters.AddWithValue("@LogDate", insertTransactionModel.LogDate);
                cn.Parameters.AddWithValue("@FailedStatus", insertTransactionModel.FailedStatus);
                cn.Parameters.AddWithValue("@FileName", insertTransactionModel.FileName);
                cn.Parameters.AddWithValue("@IsDeleted", "False");
                cn.Parameters.AddWithValue("@Acknowledge", insertTransactionModel.Acknowledge);
                adapter.InsertCommand = cn;
                adapter.InsertCommand.ExecuteNonQuery();
                cn.Dispose();
                ID++;
                Logger.Info("CallFIMIService Inser Ends. UserID:" + req);
            }
            catch (Exception ex)
            {
                Logger.Error("Insert  failed:" + "message:" + ex.Message + ".StackTrace:" + ex.StackTrace + ".Request:" + request);
            }
            finally
            {
                con.Close();
            }
            return ID;
        }
        public class CardDetails
        {
            public string AccountNumber { get; set; }
            public string AccountCurrencyCode { get; set; }
            public string Pan { get; set; }
            public string CardCurrencyCode { get; set; }
            public string Cardtype { get; set; }
            public string DateIssued { get; set; }
            public string ExpiryDate { get; set; }
            public string DateActivated { get; set; }
            public string Limit { get; set; }
            public string LastPaymentMade { get; set; }
            public string Status { get; set; }
            public Guid TransId { get; set; }
            //									
        }

        public async static Task<GetAccountFullInfoResponse> FioranoGetAccountFullInfo(string accountNumber)
        {
            var PostParam = JsonConvert.SerializeObject(accountNumber);
            GetAccountFullInfoResponse fioranoFtResponse = new GetAccountFullInfoResponse();
            try
            {
                Logger.Info("Method: FioranoGetAccountFullInfo .  " + "RequestParam: " + accountNumber);
                var client = new RestClient(ConfigurationManager.AppSettings["FioranoBaseUrl"].ToString() + "/EacbsEnquiry/GetAccountFullInfo/" + accountNumber);
                var request = new RestRequest(Method.GET);
                IRestResponse response = await client.ExecuteTaskAsync(request);
                Logger.Info("Method: FioranoGetAccountFullInfo .  " + "RequestParam: " + PostParam + "Response:" + response.Content);
                fioranoFtResponse = JsonConvert.DeserializeObject<GetAccountFullInfoResponse>(response.Content);
            }
            catch (Exception ex)
            {
                Logger.Error("Method: FioranoGetAccountFullInfo Exception. Request " + "RequestParam: " + PostParam + " .ErrorMessage: " + ex.Message + "StackTrace: " + ex.StackTrace);
            }
            return fioranoFtResponse;
        }
        public class GetAccountFullInfoResponse
        {
            public BankAccountFullInfoNew BankAccountFullInfo { get; set; }
        }
        public class BankAccountFullInfoNew
        {
            public string NUBAN { get; set; }
            public string BRA_CODE { get; set; }
            public string DES_ENG { get; set; }
            public string CUS_NUM { get; set; }
            public string CUR_CODE { get; set; }
            public string LED_CODE { get; set; }
            public object SUB_ACCT_CODE { get; set; }
            public string CUS_SHO_NAME { get; set; }
            public string AccountGroup { get; set; }
            public string CustomerStatus { get; set; }
            public string ADD_LINE1 { get; set; }
            public string ADD_LINE2 { get; set; }
            public string MOB_NUM { get; set; }
            public string email { get; set; }
            public string ACCT_NO { get; set; }
            public string MAP_ACC_NO { get; set; }
            public string ACCT_TYPE { get; set; }
            public string ISO_ACCT_TYPE { get; set; }
            public string TEL_NUM { get; set; }
            public string DATE_OPEN { get; set; }
            public string STA_CODE { get; set; }
            public string CLE_BAL { get; set; }
            public string CRNT_BAL { get; set; }
            public object BAL_LIM { get; set; }
            public string TOT_BLO_FUND { get; set; }
            public object INTRODUCER { get; set; }
            public string DATE_BAL_CHA { get; set; }
            public object NAME_LINE1 { get; set; }
            public string NAME_LINE2 { get; set; }
            public string BVN { get; set; }
            public string REST_FLAG { get; set; }
            public RESTRICTIONS RESTRICTIONS { get; set; }
            public string IsSMSSubscriber { get; set; }
            public string Alt_Currency { get; set; }
            public string Currency_Code { get; set; }
            public string T24_BRA_CODE { get; set; }
            public string T24_CUS_NUM { get; set; }
            public string T24_CUR_CODE { get; set; }
            public string T24_LED_CODE { get; set; }
            public string OnlineActualBalance { get; set; }
            public string OnlineClearedBalance { get; set; }
            public string OpenActualBalance { get; set; }
            public string OpenClearedBalance { get; set; }
            public string WorkingBalance { get; set; }
            public string CustomerStatusCode { get; set; }
            public string CustomerStatusDeecp { get; set; }
            public object LimitID { get; set; }
            public string LimitAmt { get; set; }
            public string MinimumBal { get; set; }
            public string UsableBal { get; set; }
            public string AccountDescp { get; set; }
            public string CourtesyTitle { get; set; }
            public string AccountTitle { get; set; }
            public object AMFCharges { get; set; }

        }

        public class RESTRICTIONS
        {
            public List<RESTRICTION> RESTRICTION { get; set; }
        }

        public class RESTRICTION
        {
            [JsonProperty(PropertyName = "RESTRICTION.CODE")]
            public object RESTRICTIONCODE { get; set; }

            [JsonProperty(PropertyName = "RESTRICTION.DESCRIPTION")]
            public object RESTRICTIONDESCRIPTION { get; set; }

        }
    }
}
