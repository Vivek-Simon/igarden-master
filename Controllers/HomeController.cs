using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using igarden.CustomFilter;

namespace igarden.Controllers
{
    /*Author:Pradeep Govindan
     * Student ID: 29868947
     * Created Date: 19/04/2020
     * Modified Date: 26/04/2020 */

    //Adding password protection to access the controller
    //[BasicAuthenticationAttribute("iGarden", "Password01", BasicRealm = "iGarden")]
    public class HomeController : Controller
    {
        //Function to display home page and start the application   
        //Input- None
        //Output - Index view
        public ActionResult Index()
        {
            Session["Postcode"] = null;
            Session["Address"] = null;
            Session["locLat"] = null;
            Session["locLng"] = null;
            Session["soilType"] = null;
            Session["waterContent"] = null;
            Session["Error message"] = null;
            return View();
        }

        //Function to display about us page
        //Input- None
        //Output - About us view
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }





        //Funtion to handle all API request and return the XML content
        //Input - Url of API, Parameters for API, Type of response
        //Output - XML document                   
        private XmlDocument GetAPIXMLContent(string url, string urlparameters, string mediaType = "application/xml")
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);


            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(mediaType));


            var response = client.GetAsync(urlparameters).Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var data = response.Content.ReadAsStringAsync().Result;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(data);

            return xmlDoc;
        }

        //Funtion to process the XML result of position API and return latitude and longitude of address
        //Input - address text
        //Output - Geo loaction code of the address  
        private List<string> getLongLat(string address_text)
        {
            List<string> geocode = new List<string>();
            address_text = address_text.Replace(' ', '+');
            address_text = address_text.Replace(',', '+');

            string address = address_text + "+Victoria+Australia";
            string location_URL = "https://api.opencagedata.com";
            string loc_url_parameters = "/geocode/v1/xml?q=" + address + "&key=5b390b1a35ef4d07bb8b05600a1647c4&pretty=1";


            XmlDocument xmlDoc = GetAPIXMLContent(location_URL, loc_url_parameters, "text/xml");
            if(xmlDoc==null)
            {
                return null;
            }

            XmlNodeList xnLatList = xmlDoc.SelectNodes("/response/results/result/geometry/lat");
            geocode.Add(Math.Round(double.Parse(xnLatList[0].InnerText), 7).ToString());

            XmlNodeList xnLngList = xmlDoc.SelectNodes("/response/results/result/geometry/lng");
            geocode.Add(Math.Round(double.Parse(xnLngList[0].InnerText), 7).ToString());

            XmlNodeList xnAddressList = xmlDoc.SelectNodes("/response/results/result/formatted");
            Session["Address"] = xnAddressList[0].InnerText;

            XmlNodeList xnPostcodeList = xmlDoc.SelectNodes("//response/results/result/components/postcode");
            Session["Postcode"] = xnPostcodeList[0].InnerText;

            return geocode;
        }

        //Function to process the Soil API result and return the soil type on the given location
        //Input - latitude and longitude
        //Output - soil type 
        private string GetSoilType(string lng, string lat)
        {
            string soilURL = "https://www.asris.csiro.au/ASRISApi/api/APSIM/";
            string soilurlParameters = "getApsoil?longitude=" + lng + "&latitude=" + lat;

            XmlDocument xmlDoc = GetAPIXMLContent(soilURL, soilurlParameters);
            if (xmlDoc == null)
            {
                return null;
            }

            XmlNodeList xnList = xmlDoc.SelectNodes("/ApsimSoils/Soil/SoilType");
            string soil_type = xnList[0].InnerText;

            return soil_type.Split()[0];
        }

        //Funtion to process water content API result and return the water content in the given location
        //Input - latitude and longitude
        //Output - water content 
        private int GetWaterContent(string lng, string lat)
        {
            string gWaterURL = "https://www.asris.csiro.au/ASRISApi/api/SLGA/";
            string gWaterURLParameters = "getSoilWaterParameters?longitude=" + lng + "&latitude=" + lat;

            XmlDocument xmlDoc = GetAPIXMLContent(gWaterURL, gWaterURLParameters);

            if (xmlDoc == null)
            {
                return -1;
            }

            XmlNodeList xnList = xmlDoc.SelectNodes("/SoilBuckets/SoilBucket/TotalAWCmm");
            double waterContent = Math.Round(float.Parse(xnList[0].InnerText), 1);
            
            return int.Parse(Math.Round(((waterContent - 70) * (650 / 70)) + 150).ToString()); 

        }

        //Funtion to process the places API result and return the list of near by nurseries for a given location
        //Input - latitude and longitude
        //Output - soil type 
        private List<Models.Nursery> getNearbyNursery(string lng, string lat)
        {
            List<Models.Nursery> nurseryList = new List<Models.Nursery>();

            string location_URL = "https://maps.googleapis.com";
            string loc_url_parameters = "/maps/api/place/nearbysearch/xml?location=" + lat + "," + lng + "&radius=3000&name=plant+nursery&key=AIzaSyB8GjoBCD4Pmc_MR_YjUF3Y7QBrcS1BvrU";

            XmlDocument xmlDoc = GetAPIXMLContent(location_URL, loc_url_parameters, "text/xml");

            XmlNodeList resultNameList = xmlDoc.SelectNodes("/PlaceSearchResponse/result");
            if (xmlDoc == null)
            {
                return null;
            }

            for (int i = 0; i < resultNameList.Count && i<5; i++)
            {
                var result = resultNameList[i];
                string name = result["name"].InnerText;
                string address = result["vicinity"].InnerText;
                string rating = result["rating"].InnerText;
                string nlat = result["geometry"]["location"]["lat"].InnerText;
                string nlng = result["geometry"]["location"]["lng"].InnerText;

                Models.Nursery NurseryDetails = new Models.Nursery();
                NurseryDetails.ID = "nursery"+i.ToString();
                NurseryDetails.Name = name;
                NurseryDetails.Address = address;
                NurseryDetails.Latitude = double.Parse(nlat);
                NurseryDetails.Longitude = double.Parse(nlng);
                NurseryDetails.Rating = float.Parse(rating);
                nurseryList.Add(NurseryDetails);
            }

            return nurseryList;
        }

        //Funtion to display list of Nearby nurseries and return the view
        //Input - None
        //Output - Nearby nurseries view 
        public ActionResult NearbyNurseries()
        {
            string lat = (string)Session["locLat"];
            string lng = (string)Session["locLng"];
            if (lat == null)
            {
                Session["Error message"] = "Address not entered correctly.";
                return View("Error");
            }
            else if (lng == null)
            {
                Session["Error message"] = "Address not entered correctly.";
                return View("Error");
            }
            Models.NearByNurseries nurseryinfo = new Models.NearByNurseries();
            nurseryinfo.locLat = double.Parse(lat);
            nurseryinfo.locLng = double.Parse(lng);
            nurseryinfo.Nurseries = getNearbyNursery(lng, lat);
            if(nurseryinfo.Nurseries==null)
            {
                Session["Error message"] = "Nearby Nursery Identification Failed.";
                return View("Error");
            }
            return View(nurseryinfo);
        }

        //Funtion to handle process the resuest and redirect based on the type of the request
        //Input - Form data, Plants option, Nurseries option.
        //Output - Direct to corresponding function. 
        public ActionResult getDetails(FormCollection frm, string plants, string nurseries)
        {
            var geocode = getLongLat(frm["address"]);
            if (geocode == null)
            {
                Session["Error message"] = "Address not entered correctly. Location Identification Failed.";
                return View("Error");
            }

            string lat = geocode[0];
            string lng = geocode[1];
            Session["locLat"] = lat;
            Session["locLng"] = lng;

            if (!string.IsNullOrEmpty(plants))
            {
                var soilType = GetSoilType(geocode[1], geocode[0]);
                if (soilType == null)
                {
                    Session["Error message"] = "Soil Type Identification Failed.";
                    return View("Error");
                }

                int waterContent = GetWaterContent(geocode[1], geocode[0]);
                if (waterContent==-1)
                {
                    Session["Error message"] = "Water Resource Identification Failed.";
                    return View("Error");
                }
             
                Session["soilType"] = soilType;
                Session["waterContent"] = waterContent.ToString();

                return RedirectToAction("Index", "Plants");
            }
            else
            {               
                return RedirectToAction("NearbyNurseries","Home");
            }
        }
    }
}