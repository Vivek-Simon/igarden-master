using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Migrations.Model;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using igarden.CustomFilter;

namespace igarden.Controllers
{
    /*Author:Pradeep Govindan
    * Student ID: 29868947
    * Created Date: 21/05/2020
    * Modified Date: 23/05/2020 */

    //Adding password protection to access the controller
    [BasicAuthenticationAttribute("iGardenAdmin", "IntelliGarden22", BasicRealm = "iGarden")]
    public class DataController : Controller
    {
        //Function
        //Function to upload data to database
        //Input - None
        //Output - Success or error page.
        public ActionResult Index()
        {
            string path = Server.MapPath("~/Data/");

            //Upload allergy data
            int allergydb  = allergyTable(path);

            //Upload plant data
            int plantdb = plantTable(path);

            //return success page on successful opload
            if (allergydb == 1 && plantdb == 1)
            {
                return View("DataUploadSuccess");
            }
            //return error on failure
            Session["Error message"] = "Data Upload Failed.";
            return View("Error");
        }
        //Function to upload allergy table
        //Input - File path
        //Output - 1(upload success) /0(fail)
        private int allergyTable(string path)
        {
            string fileName = "Allergy.csv";
            string filePath = path + fileName;
            if (System.IO.File.Exists(filePath))
            {
                //Create a DataTable.
                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[9] { new DataColumn("Scientific_Name", typeof(string)),
                                new DataColumn("Common_name", typeof(string)),
                                new DataColumn("Very_poisonous",typeof(int)),
                                new DataColumn("Poisonous",typeof(int)),
                                new DataColumn("Allergenic",typeof(int)),
                                new DataColumn("Irritant",typeof(int)),
                                new DataColumn("Invasive",typeof(int)),
                                new DataColumn("Plant_id",typeof(string)),
                                new DataColumn("Image_src",typeof(string)) });

                string destTable = "AllergyDB";
                return CreateDatabase(filePath, dt, destTable);
            }
            return 0;
        }
        //Function to upload plant table
        //Input - File path
        //Output - 1(upload success) /0(fail)
        private int plantTable(string path)
        {
            string fileName = "Plant_Image.csv";
            string filePath = path + fileName;
            if (System.IO.File.Exists(filePath))
            {
                //Create a DataTable.
                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[19] {                                                        
                                new DataColumn("Type",typeof(string)),
                                new DataColumn("Genus",typeof(string)),
                                new DataColumn("Tube_colour",typeof(string)),
                                new DataColumn("Botanical_name",typeof(string)),
                                new DataColumn("Common_name", typeof(string)),
                                new DataColumn("Rain",typeof(int)),
                                new DataColumn("ph",typeof(string)),
                                new DataColumn("min_height",typeof(decimal)),
                                new DataColumn("max_height",typeof(decimal)),
                                new DataColumn("min_width",typeof(decimal)),
                                new DataColumn("max_width",typeof(decimal)),
                                new DataColumn("Soil",typeof(string)),
                                new DataColumn("Flower_Season",typeof(string)),
                                new DataColumn("Flower_Colour",typeof(string)),
                                new DataColumn("Frost_level",typeof(string)),
                                new DataColumn("Scientific_Name", typeof(string)),
                                new DataColumn("Fire_resistant",typeof(string)),
                                new DataColumn("Plant_id",typeof(string)),
                                new DataColumn("Image_src",typeof(string))});

                string destTable = "PlantDB";
                return CreateDatabase(filePath, dt, destTable);
            }
            return 0;
        }

        //Function to load data to database
        //Input - filepath, datatable, destination tabe name
        //Output - 1 on success
        private int CreateDatabase(string filePath, DataTable dt, string destTable)
        {
            //Read the contents of CSV file.
            string csvData = System.IO.File.ReadAllText(filePath);

            //Execute a loop over the rows.
            foreach (string row in csvData.Split('\n').Skip(1))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    dt.Rows.Add();
                    int i = 0;

                    //Execute a loop over the columns.
                    foreach (string cell in row.Split(','))
                    {
                        dt.Rows[dt.Rows.Count - 1][i] = cell.Trim();
                        i++;
                    }
                }
            }

            
            string ddlCommand = CreateTable(dt, destTable);
            string conString = ConfigurationManager.ConnectionStrings["dbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(conString))
            {
                con.Open();

                //delete table if exist
                try
                {
                    SqlCommand delcmd = new SqlCommand(" DROP TABLE " + destTable, con);
                    int delresult = delcmd.ExecuteNonQuery();
                }
                catch
                {
                    //do nothing 
                }
                //Create Table
                SqlCommand mySqlCommand = con.CreateCommand();
                mySqlCommand.CommandText = ddlCommand;
                int result = mySqlCommand.ExecuteNonQuery();



                using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                {
                    //Set the database table name.
                    sqlBulkCopy.DestinationTableName = destTable;


                    sqlBulkCopy.WriteToServer(dt);
                    con.Close();
                }
                return 1;
            }
            
        }
    
               
        public static string CreateTable(DataTable dt, string tableName)
        {
            /*Author:Vivek Simon
             * Student ID: 29557380
             * Created Date: 21/04/2020
             * Modified Date: 22/04/2020 
             *Functionality: Used to generate create table statements so that it can be uploaded into the SQL server*/
            string ddlstatement;
            ddlstatement = "CREATE TABLE " + tableName + "(";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                ddlstatement += "[" + dt.Columns[i].ColumnName + "] ";

                if (dt.Columns[i].ColumnName == "Plant_id")
                {
                    ddlstatement += " nvarchar(10) PRIMARY KEY NOT NULL";
                }
                else
                {
                    string columnType = dt.Columns[i].DataType.ToString();
                    switch (columnType)
                    {
                        case "System.Int32":
                            ddlstatement += " int ";
                            break;
                        case "System.Int64":
                            ddlstatement += " bigint ";
                            break;
                        case "System.Int16":
                            ddlstatement += " smallint";
                            break;
                        case "System.Byte":
                            ddlstatement += " tinyint";
                            break;
                        case "System.Decimal":
                            ddlstatement += " decimal ";
                            break;
                        case "System.DateTime":
                            ddlstatement += " datetime ";
                            break;
                        case "System.String":
                        default:
                            ddlstatement += string.Format(" nvarchar({0}) ", dt.Columns[i].MaxLength == -1 ? "max" : dt.Columns[i].MaxLength.ToString());
                            break;
                    }
                }
                if (dt.Columns[i].AutoIncrement)
                    ddlstatement += " IDENTITY(" + dt.Columns[i].AutoIncrementSeed.ToString() + "," + dt.Columns[i].AutoIncrementStep.ToString() + ") ";
                if (!dt.Columns[i].AllowDBNull)
                    ddlstatement += " NOT NULL ";
                ddlstatement += ",";
            }
            return ddlstatement.Substring(0, ddlstatement.Length - 1) + "\n)";
        }
    }
}