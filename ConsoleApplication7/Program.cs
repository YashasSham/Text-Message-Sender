using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace ExotelSDK
{
    public class SendSMS
    {
        private string SID = null;
        private string token = null;

        public SendSMS(string SID, string token)
        {
            this.SID = SID;
            this.token = token;
        }

        /*************************************Method to send the message**********************************************/
        /*
         * Takes "From","To" and "Body" of the message as the input
         * returns "sid" of the message*/

        public string Send(string from, string to, string Body)
        {
            Dictionary<string, string> postValues = new Dictionary<string, string>();
            postValues.Add("From", from);
            postValues.Add("To", to);
            postValues.Add("Body", Body);

            String postString = "";

            foreach (KeyValuePair<string, string> postValue in postValues)
            {
                postString += postValue.Key + "=" + HttpUtility.UrlEncode(postValue.Value) + "&";
            }
            postString = postString.TrimEnd('&');


            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };
            string smsURL = "https://twilix.exotel.in/v1/Accounts/"+ this.SID+ "/Sms/send";      //url to which post has to be done
            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(smsURL);
            objRequest.Credentials = new NetworkCredential(this.SID, this.token);
            objRequest.Method = "POST";
            objRequest.ContentLength = postString.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";

            // post data is sent as a stream                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                
            StreamWriter opWriter = null;
            opWriter = new StreamWriter(objRequest.GetRequestStream());
            opWriter.Write(postString);
            opWriter.Close();

            // returned values are returned as a stream, then read into a string                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            
            HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
            string postResponse = null;
            using (StreamReader responseStream = new StreamReader(objResponse.GetResponseStream()))
            {
                postResponse = responseStream.ReadToEnd();
                responseStream.Close();
            }
            //Console.WriteLine(postResponse);

            Save(Extract(postResponse, "Sid"),"queued");            //Save this data in the json file(input.json) also//extract the Sid of the message from the Response(returned as a xml string)

            return Extract(postResponse, "Sid");                    //returns the Sid
            

        }
        /***************************************Send Method Ends*********************************************************/

        /***************************Method to get Status of previous SMSes***********************************************/
        /*
         * Provides the Stattus of a  message also other thing like "From","To" and "Body" can also be retrived
         * Intakes the "Sid of the message" and tags to be obtained;Tags="From"|"To"|"Body"|"Status"|or anything(but it will only return status if it is anything else!!!;))
         * returns either "From" or "To" or "Status" or "body" and nothing else*/

        public string Status(string Sid,string Tags)
        {

            HttpWebRequest smsStatus = (HttpWebRequest)WebRequest.Create("https://" + this.SID + ":" + this.token + "@twilix.exotel.in/v1/Accounts/mudflap1/SMS/Messages/" + Sid);           //url for http request
            smsStatus.Credentials = new NetworkCredential(this.SID, this.token);                        //credentials like "Sid of the user" and "Token of the user"
            smsStatus.Method = "GET";                                                                   //this has to be a post request

            HttpWebResponse smsStatusResponse = (HttpWebResponse)smsStatus.GetResponse();
            string postResponse = null;
            using (StreamReader responseStream = new StreamReader(smsStatusResponse.GetResponseStream()))
            {
                postResponse = responseStream.ReadToEnd();
                responseStream.Close();
            }
            if (Tags == "From")                                                             
            {
                return Extract(postResponse, "From");                               //returns From
            }
            else if (Tags == "To")
            {
                return Extract(postResponse, "To");                                 //returns To
            }
            else if (Tags == "Body")
            {
                return Extract(postResponse, "Body");                               //return Body
            }
            else
            {
                return Extract(postResponse, "Status");                             //returns status
            }
        }

        /*************************************************Status method Ends******************************************************************/

        /*************************************Method to Extract Data Between the XML tags***************************************************/
        /*
         * Response is given as a stream of xml data..data between the tags are extracted using this method
         */

        public string Extract(string input, string tags)
        {
            int pFrom;
            int pTo;
            switch (tags)
            {
                case "Sid":     pFrom = input.IndexOf("<Sid>") + "<Sid>".Length;                    //"Sid of the message!important to refer to a perticular sms"
                                pTo = input.LastIndexOf("</Sid>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;

                case "From":    pFrom = input.IndexOf("<From>") + "<From>".Length;                  //from number(Our number)
                                pTo = input.LastIndexOf("</From>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;

                case "To":      pFrom = input.IndexOf("<To>") + "<To>".Length;                      //To number(Client number)
                                pTo = input.LastIndexOf("</To>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;
                                
                case "Body":    pFrom = input.IndexOf("<Body>") + "<Body>".Length;                  //Body of the sms or the message :required to resend a message if previous has failed
                                pTo = input.LastIndexOf("</Body>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;

                case "DateSent":pFrom = input.IndexOf("<DateSent>") + "<DateSent>".Length;          //to extract the date and time of the message delivery !not used anywhere
                                pTo = input.LastIndexOf("</DateSent>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;

                case "Status":  pFrom = input.IndexOf("<Status>") + "<Status>".Length;              //Status of the sms with the given Sid can be ---queued,sending,sent,failed(has to be resent)and failed-DND(can't do anythin but send a transactional message) NOTICE:all are small letters
                                pTo = input.LastIndexOf("</Status>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;

                case "ApiVersion": pFrom = input.IndexOf("<ApiVersion>") + "<ApiVersion>".Length;   //really useless
                                pTo = input.LastIndexOf("</ApiVersion>");
                                if (pTo - pFrom >= 0)
                                    return input.Substring(pFrom, pTo - pFrom);
                                else
                                    return null;
            }

            return null;
        }
        /********************************************Extract method ends*******************************************************************/

        /************************************method to save the data***********************************************************************/
        /*
         * saves the "Sid"  and its Status in a Json file(---Project_name/bin/Debug/input.json)(yeah! kind of database for the storage)
         * It only adds new values to update use "Update" method
         */

        public void Save(string SID, string Status)
        {
            string json = File.ReadAllText("input.json");
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            //incrementin the count

            int i = Convert.ToInt32(jsonObj["Sid"][0]["Count"]);
            i++;
            jsonObj["Sid"][0]["Count"] = Convert.ToString(i);
            string j = jsonObj["Sid"][0]["Count"];

            //incrementation ends here

            jsonObj["Sid"][0][j] = SID;                                                ////////saves the Sid andthe Status of the message
            jsonObj["Status"][0][j] = Status;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("input.json", output);

            //Console.WriteLine(jsonObj.Sid[0]["1"]);
           
        }

        /***********************************Save Method ends here*****************************************************/

        /********************************check the previous messages****************************************************/
        /*
         * this method checks if the previosly sent message have been delivered or not
         * time consuming..so,Best to perform every 2-3 hours
         * no return*/

        public void check()
        {
            string json = File.ReadAllText("input.json");
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);              

            int i = Convert.ToInt32(jsonObj["Sid"][0]["Count"]);                //reads the "Count" value to get the number of messages sent
       
            Console.WriteLine(i);
            Console.ReadLine();
            SendSMS s = new SendSMS(this.SID, this.token);
            
            for (int j = 1; j <= i;j++ )
            {
                string a = Convert.ToString(j);
                string b=jsonObj["Sid"][0][a];

                if (jsonObj["Status"][0][a] == "queued"|| jsonObj["Status"][0][a] == "sending")          //if the sms is being queued or sent we cant do anything but wait to get status changed
                {  

                    string srg = s.Status(b, "Status");
                    Console.WriteLine(srg);

                    if (jsonObj["Status"][0][a] == "failed")                                              //if this changed status is "failed" we have to resend it
                    {
                        s.Send(s.Status(jsonObj["Sid"][0][a], "From"), s.Status(jsonObj["Sid"][0][a], "To"), s.Status(jsonObj["Sid"][0][a], "Body"));
                        jsonObj["Status"][0][a] = "sent";
                        s.Save(b, jsonObj["Status"][0][a]);
                    }
                    else
                    {
                        s.update(a, srg);                                                                   //other possible outcomes would be sent,queued,failed_DND or sending
                                                                                                            //so just write the status in the json file(input.json)
                    }

                }
                else if (jsonObj["Status"][0][a] == "failed_dnd")
                {
                    Console.WriteLine("the user has activated DND");                                        //If the account is updated to payed one then we can resend it as transactional
                }

            }
        }

        /********************************check method ends here*****************************************************/

        /*******************************Update method to update the Jsson data**************************************/
        /*
         * just updates a specific field(i) of the Json file(input.txt)
         * */

        public void update(string i,string Status)
        {
            string json = File.ReadAllText("input.json");
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

         
            ////////saves the Sid andthe Status of the message
            jsonObj["Status"][0][i] = Status;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("input.json", output);

            //Console.WriteLine(jsonObj.Sid[0]["1"]);
        }

        /***************************************Update method ends**********************************************************/ 
        /*
         * CALLABLE METHODS.
         * 
         *  public string Send(string from, string to, string Body)          Important//to send a message
            public string Status(string Sid,string Tags)                     //to check the status
            public string Extract(string input, string tags)                 //to Extract data from xml file
            public void Save(string SID, string Status)                      //to save the data in json file
	        public void check()                                              //to check the status of previously sent SMSes and resend it if it has failed
	        public void update(string i,string Status)                       //to update the json File   
        */


        public static void Main(string[] args)
        {
            SendSMS s = new SendSMS("mudflap1", "2339b0600aafd144542ba6b5b538f13054de3ee1");
            /*
            string Sid=s.Send("09243422233", "7411894986", "hello! how are u?.");

            string Status=s.Status(Sid,"Status");
            Console.WriteLine("the message id is" + Sid+ "and the message is queued to be sent");
            Console.WriteLine("Check after some time for confirmation");
            */
            s.check();                        //to be used after some time of sending the message
        }

    }
}