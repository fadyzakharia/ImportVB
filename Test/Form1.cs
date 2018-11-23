using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Evaluation_LoadPatient
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".csv";
            ofd.Filter = "Comma Separated (*.csv)|*.csv";
            ofd.ShowDialog();

            txtFileName.Text = ofd.FileName;
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            DataTable importData = GetDataFromFile();

            if (importData == null) return;

            SaveImportDataToDatabase(importData);

            MessageBox.Show("Import Complete !!!");

            txtFileName.Text = String.Empty;

            Cursor = Cursors.Default;
        }

        private DataTable GetDataFromFile()
        {
            DataTable importedData = new DataTable();

            try
            {
                using (StreamReader sr = new StreamReader(txtFileName.Text))
                {
                    string header = sr.ReadLine();

                    if (string.IsNullOrEmpty(header))
                    {
                        MessageBox.Show("No file data !!!");
                        return null;
                    }

                    string[] headerColumns = header.Split(new string[] { "\";\"" }, StringSplitOptions.None);

                    foreach (string headerColumn in headerColumns)
                    {
                        //headerColumn.Replace('"', ' ');
                        string headerColumnC = headerColumn.Trim('"');
                        //importedData.Columns.Add(headerColumn.Replace('"', ' '));
                        importedData.Columns.Add(headerColumnC);
                        //MessageBox.Show(headerColumnC);
                    }

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        //Console.WriteLine(line);
                        if (string.IsNullOrEmpty(line)) continue;

                        string[] fields = line.Split(new string[] { "\";\"" }, StringSplitOptions.None);

                        DataRow importedRow = importedData.NewRow();

                        for (int i = 0; i < fields.Count(); i++)
                        {
                            importedRow[i] = fields[i];
                            importedRow[i] = fields[i].Trim('"');
                        }
                        importedData.Rows.Add(importedRow);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read !!!");
                Console.WriteLine(e.Message);
            }
            return importedData;
        }

        private void SaveImportDataToDatabase(DataTable importData)
        {
            string connectionString = "Data Source=FADY-PC\\SQLEXPRESS;Initial Catalog=integrationTest;Integrated Security=True";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                try
                {
                    /**********************************************************************************/
                    /**********************************************************************************/
                    // Insert table Patient
                    foreach (DataRow importRow in importData.Rows)
                    {
                        SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth,NAM,NAMExpiryDate,Note,Mother_FirstName,Mother_LastName,Father_FirstName,Father_LastName,Email,Address,Country,ZipCode,GenderLookup,MaritalStatusLookup,LanguageLookup,StatusLookup,Identifier03)" +
                                                        "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth,@NoAssMaladie,@Expiry_AM,@Note,@MotherFirstName,@MotherLastname,@FatherFirstName,@FatherLastname,@Email,@Address,@Country,@ZipCode,@GenderLookup,@MaritalStatusLookup,@LanguageLookup,@StatusLookup,@Identifier03)",
                                                        conn);

                        cmd.Parameters.AddWithValue("@PatientGuid", importRow["PatientGuid"]);

                        // First Name & Last Name
                        string s = Convert.ToString(importRow["FullName"].ToString());
                        // Split string on spaces.
                        string fn = "";
                        string ln = "";
                        if (importRow["FirstName"].ToString() == "" || importRow["LastName"].ToString() == "")
                        {
                            //MessageBox.Show("1 !!!");
                            string[] words = s.Split(' ');
                            foreach (string word in words)
                            {
                                ln = words[0].Replace(',', ' ');
                                fn = words[1].Replace(',', ' ');
                            }
                        }
                        else
                        {
                            fn = Convert.ToString(importRow["FirstName"]);
                            ln = Convert.ToString(importRow["LastName"]);
                        }

                        cmd.Parameters.AddWithValue("@FirstName", fn);
                        cmd.Parameters.AddWithValue("@LastName", ln);

                        // DOB
                        string inputString = importRow["Date Of Birth"].ToString();

                        DateTime dDate;
                        DateTime dob;

                        if (DateTime.TryParse(inputString, out dDate))
                        {
                            dob = DateTime.Parse(inputString);
                        }
                        else
                        {
                            dob = DateTime.MinValue;
                        }

                        if (dob < DateTime.Parse(System.Data.SqlTypes.SqlDateTime.MinValue.ToString()) || dob > DateTime.Parse(System.Data.SqlTypes.SqlDateTime.MaxValue.ToString()))
                        {
                            cmd.Parameters.AddWithValue("@Date_Of_Birth", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Date_Of_Birth", dob);
                        }

                        cmd.Parameters.AddWithValue("@NoAssMaladie", importRow["NoAssMaladie"]);

                        if (importRow["NoAssMaladie"].ToString() == "")
                        {
                            cmd.Parameters.AddWithValue("@Expiry_AM", DBNull.Value);
                            cmd.Parameters.AddWithValue("@GenderLookup", DBNull.Value);
                        }
                        else
                        {
                            string expiry_am = importRow["Exp.year"] + "-" + importRow["Exp.Month"] + "-01";
                            DateTime expiry_am1 = DateTime.Parse(expiry_am);
                            cmd.Parameters.AddWithValue("@Expiry_AM", expiry_am1);
                            
                            int sex_num = 0;
                            string sex = importRow["NoAssMaladie"].ToString().Substring(6, 2);
                            sex_num = Convert.ToInt32(sex);
                            //MessageBox.Show(sex_num.ToString());
                            if (sex_num > 50)
                            {
                                SqlCommand command = new SqlCommand("SELECT Id FROM Lookup where Type = 'Gender' and FR = 'Femme' ", conn);
                                
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            //MessageBox.Show(reader[0].ToString());
                                            cmd.Parameters.AddWithValue("@GenderLookup", reader[0].ToString());
                                        }
                                    }
                            }
                            else
                            {
                                SqlCommand command = new SqlCommand("SELECT Id FROM Lookup where Type = 'Gender' and FR = 'Homme' ", conn);

                                using (SqlDataReader reader = command.ExecuteReader())
                                {

                                    while (reader.Read())
                                    {
                                        //MessageBox.Show(reader[0].ToString());
                                        cmd.Parameters.AddWithValue("@GenderLookup", reader[0].ToString());
                                    }
                                }
                            }
                        }
                        
                        // LookUp Statut Marital
                        if (importRow["Civil State"].ToString() == "")
                        {

                            cmd.Parameters.AddWithValue("@MaritalStatusLookup", DBNull.Value);
                            //MessageBox.Show("Empty MS");
                        }
                        else
                        {
                            string ms = importRow["Civil State"].ToString().Substring(0, 1);
                            
                            //MessageBox.Show(ms);
                            string q_ms = "";
                            if(ms[0] =='D')
                            {
                                q_ms = "SELECT Id FROM Lookup where Type = 'MARITALSTATUS' and En = 'Divorced' ";
                            }
                            else if(ms[0] =='M')
                            {
                                q_ms = "SELECT Id FROM Lookup where Type = 'MARITALSTATUS' and En = 'Married' ";
                            }
                            else if (ms[0] =='C' || ms[0] =='U')
                            {
                                q_ms = "SELECT Id FROM Lookup where Type = 'MARITALSTATUS' and En = 'Common-Law Union' ";
                            }
                            else if(ms[0] =='S')
                            {
                                q_ms = "SELECT Id FROM Lookup where Type = 'MARITALSTATUS' and En = 'Separated' ";
                            }
                            else if (ms[0] =='V' || ms[0] == 'W')
                            {
                                q_ms = "SELECT Id FROM Lookup where Type = 'MARITALSTATUS' and En = 'Widowed' ";
                            }
                            //MessageBox.Show(q_ms);
                            SqlCommand command1 = new SqlCommand(q_ms, conn);
                            using (SqlDataReader reader1 = command1.ExecuteReader())
                            {

                                while (reader1.Read())
                                {
                                    //MessageBox.Show(reader1[0].ToString());
                                    cmd.Parameters.AddWithValue("@MaritalStatusLookup", reader1[0].ToString());
                                }
                            }
                        }

                        // LanguageLookup
                        string q_lan = "";
                        if (importRow["Langue"].ToString() == "")
                        {
                            q_lan = "SELECT Id FROM Lookup where Type = 'LANGUAGE' and En = 'French' ";
                        }
                        else
                        {
                            string lan = importRow["Langue"].ToString().Substring(0, 1);

                            //MessageBox.Show(ms);
                            
                            if (lan[0] == 'F')
                            {
                                q_lan = "SELECT Id FROM Lookup where Type = 'LANGUAGE' and En = 'French' ";
                            }
                            else if (lan[0] == 'E' || lan[0] == 'A')
                            {
                                q_lan = "SELECT Id FROM Lookup where Type = 'LANGUAGE' and En = 'English' ";
                            }
                            else
                            {
                                q_lan = "SELECT Id FROM Lookup where Type = 'LANGUAGE' and En = 'French' ";
                            }
                        }
                        SqlCommand command2 = new SqlCommand(q_lan, conn);
                        using (SqlDataReader reader2 = command2.ExecuteReader())
                        {

                            while (reader2.Read())
                            {
                                //MessageBox.Show(reader1[0].ToString());
                                cmd.Parameters.AddWithValue("@LanguageLookup", reader2[0].ToString());
                            }
                        }
                        // End Language

                        // StatusLookup
                        string q_stat = "";
                        if (importRow["IsDead"].ToString() == "Y")
                        {
                            q_stat = "SELECT Id FROM Lookup where Type = 'STATUS' and En = 'Deceased' ";
                        }
                        else
                        {
                            if(importRow["Archived"].ToString() == "Y")
                            {
                                q_stat = "SELECT Id FROM Lookup where Type = 'STATUS' and En = 'Deactivated' ";
                            }
                            else 
                            {
                                q_stat = "SELECT Id FROM Lookup where Type = 'STATUS' and En = 'Actif' ";
                            }
                        }
                        SqlCommand command3 = new SqlCommand(q_stat, conn);
                        using (SqlDataReader reader3 = command3.ExecuteReader())
                        {
                            while (reader3.Read())
                            {
                                //MessageBox.Show(reader1[0].ToString());
                                cmd.Parameters.AddWithValue("@StatusLookup", reader3[0].ToString());
                            }
                        }
                        // End StatusLookup

                        cmd.Parameters.AddWithValue("@Note", importRow["Note"]);

                        cmd.Parameters.AddWithValue("@MotherFirstName", importRow["MotherFirstName"]);

                        cmd.Parameters.AddWithValue("@MotherLastname", importRow["MotherLastname"]);
                        cmd.Parameters.AddWithValue("@FatherFirstName", importRow["FatherFirstName"]);
                        cmd.Parameters.AddWithValue("@FatherLastname", importRow["FatherLastname"]);
                        cmd.Parameters.AddWithValue("@Email", importRow["Email"]);
                        cmd.Parameters.AddWithValue("@Address", importRow["Address"]);
                        cmd.Parameters.AddWithValue("@Country", importRow["Country"]);
                        cmd.Parameters.AddWithValue("@ZipCode", importRow["ZipCode"]);

                        /**********************************************************************************/
                        //Dictionnaire
                        string connectionString1 = "Data Source=FADY-PC\\SQLEXPRESS;Initial Catalog=Dictionnary;Integrated Security=True";

                        using (SqlConnection conn1 = new SqlConnection(connectionString1))
                        {
                            conn1.Open();

                            if (importRow["FamilyDoctor"].ToString().Trim() != "")
                            {
                                //MessageBox.Show(importRow["FamilyDoctor"].ToString());
                                string qr_doc = "SELECT Top 1 concat(FirstName,' ',LastName) as doctor " +
                                           "FROM Doctors " +
                                           "where substring(convert(varchar(10), RamqId), 2, 5) = " + importRow["FamilyDoctor"] + "";
                                //MessageBox.Show(qr_doc);
                                SqlCommand command_doc = new SqlCommand(qr_doc, conn1);

                                // MessageBox.Show(count.ToString());

                                using (SqlDataReader reader_doc = command_doc.ExecuteReader())
                                {
                                    int numberOfRecordsDoc = 0;
                                    while (reader_doc.Read())
                                    {
                                        numberOfRecordsDoc++;
                                        //MessageBox.Show(reader_doc[0].ToString()+'='+importRow["PatientGuid"]);
                                        cmd.Parameters.AddWithValue("@Identifier03", reader_doc[0].ToString());

                                    }

                                    if (numberOfRecordsDoc == 0)
                                    {
                                        cmd.Parameters.AddWithValue("@Identifier03", DBNull.Value);
                                    }
                                }
                                command_doc.ExecuteNonQuery();
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@Identifier03", DBNull.Value);
                            }
                            conn1.Close();
                        }

                        /**********************************************************************************/
                        cmd.ExecuteNonQuery();
                        // End Insert Table Patient

                        // Risk Factor
                        if (importRow["Smoke"].ToString() == "Y" || importRow["SmokeInThePast"].ToString() == "Y" || importRow["TakeDrugs"].ToString() == "Y")
                        {

                            try
                            {
                                var guid = Guid.NewGuid().ToString();
                                string txtRF = "";
                                //MessageBox.Show(importRow["PatientGuid"].ToString());


                                SqlCommand cmd_rf = new SqlCommand("insert into RiskFactor(Id,PatientId,RiskFactor)" +
                                                                   "VALUES(@RFId,@RFPatientId,@RiskFactor)",
                                                                    conn);
                                cmd_rf.Parameters.AddWithValue("@RFId", guid.ToString());
                                cmd_rf.Parameters.AddWithValue("@RFPatientId", importRow["PatientGuid"]);

                                if (importRow["Smoke"].ToString() == "Y")
                                {
                                    if (importRow["SmokeInThePast"].ToString() == "Y" || importRow["SmokeInThePast"].ToString() == "Y")
                                        txtRF = "Smoke /";
                                    else
                                        txtRF = "Smoke ";
                                }
                                if (importRow["SmokeInThePast"].ToString() == "Y")
                                {
                                    txtRF = "Smoke /";
                                    if (importRow["SmokeInThePast"].ToString() == "Y")
                                        txtRF = txtRF + " Smoke In The Past /";
                                    else
                                        txtRF = txtRF + " Smoke In The Past ";
                                }
                                if (importRow["SmokeInThePast"].ToString() == "Y")
                                {
                                    txtRF = txtRF + " Take Drugs";
                                }

                                cmd_rf.Parameters.AddWithValue("@RiskFactor", txtRF);
                                cmd_rf.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error Risk Factor !!!");
                                Console.WriteLine(e.Message);
                            }
                        }
                        // End Risk Factor

                        // Phone Number
                        string[] arr_phone = new string[3];
                        arr_phone[0] = importRow["Phone1"].ToString();
                        arr_phone[1] = importRow["Phone2"].ToString();
                        arr_phone[2] = importRow["Phone3"].ToString();

                        string[] arr_notePhone = new string[3];
                        arr_notePhone[0] = importRow["PhoneNote1"].ToString();
                        arr_notePhone[1] = importRow["PhoneNote2"].ToString();
                        arr_notePhone[2] = importRow["PhoneNote3"].ToString();

                       var i = 0;
                       foreach (var item in arr_phone)
                       {
                           if (item.Trim() != "")
                           {
                               SqlCommand cmd_phone = new SqlCommand("insert into PhoneNumber(Id,PatientId,Number,TypeLookup,Note,IsPreferred)" +
                                                        "VALUES(@IdPhoneNumber,@PatientGuidPhone,@NumberPhone,@TypeLookup,@NotePhone,@IsPreferred)",
                                                        conn);
                               var guid = Guid.NewGuid().ToString();
                               cmd_phone.Parameters.AddWithValue("@IdPhoneNumber", guid.ToString());
                               cmd_phone.Parameters.AddWithValue("@PatientGuidPhone", importRow["PatientGuid"]);
                               cmd_phone.Parameters.AddWithValue("@NumberPhone", item);

                               int preferred;
                               if (i == 0)
                                   preferred = 1;
                               else
                                   preferred = 0;
                               cmd_phone.Parameters.AddWithValue("@IsPreferred", preferred);
                               
                               // Phone = Item
                               if (arr_notePhone[i].Trim() == "")
                               {
                                   cmd_phone.Parameters.AddWithValue("@NotePhone", DBNull.Value);
                                   cmd_phone.Parameters.AddWithValue("@TypeLookup", DBNull.Value);
                               }
                               else
                               {
                                   cmd_phone.Parameters.AddWithValue("@NotePhone", arr_notePhone[i]);
                                   //cmd_phone.Parameters.AddWithValue("@TypeLookup", DBNull.Value);

                                   string search_type = Regex.Replace(arr_notePhone[i], @"(\s+|@|&|'|\(|\)|<|>|#)", "");

                                   string qr = "SELECT top 1 Id FROM Lookup where Type = 'PhoneType' and (Fr like '%" + search_type + "%' or En like '%" + search_type + "%') ";
                                   //MessageBox.Show(qr);
                                   SqlCommand command_type = new SqlCommand(qr, conn);

                                  // MessageBox.Show(count.ToString());

                                   using (SqlDataReader reader_type = command_type.ExecuteReader())
                                   {
                                       int numberOfRecords = 0;
                                       while (reader_type.Read())
                                       {
                                           numberOfRecords++;
                                           //MessageBox.Show(reader_type[0].ToString()+'='+importRow["PatientGuid"]);
                                           cmd_phone.Parameters.AddWithValue("@TypeLookup", reader_type[0].ToString());
                                           
                                       }

                                       if (numberOfRecords == 0)
                                       {
                                           cmd_phone.Parameters.AddWithValue("@TypeLookup", DBNull.Value);
                                       }
                                   }
                               }
                               //MessageBox.Show(importRow["PatientGuid"] + "===>" + item + "===" + arr_notePhone[i]);
                               i++;

                               cmd_phone.ExecuteNonQuery();
                           }
                       }
                       // End Phone Number
                       

                    }
                    // End 
                    /**********************************************************************************/
                    /**********************************************************************************/
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Data !!!");
                    Console.WriteLine(e.Message);
                }
                conn.Close();
            }
        }
    }
    /*
    internal class Patient
    {
        public string FirstName;
        public string LastName;
    }*/
}