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
                using(StreamReader sr = new StreamReader(txtFileName.Text))
                {
                    string header = sr.ReadLine();
                    
                    if(string.IsNullOrEmpty(header))
                    {
                        MessageBox.Show("No file data !!!");
                        return null;
                    }

                    string[] headerColumns = header.Split(';');

                    foreach(string headerColumn in headerColumns)
                    {
                        importedData.Columns.Add(headerColumn);
                    }

                    while(!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        //Console.WriteLine(line);
                        if (string.IsNullOrEmpty(line)) continue; 

                        string[] fields = line.Split(';');

                        DataRow importedRow = importedData.NewRow();

                        for(int i = 0;i < fields.Count();i++)
                        {
                            importedRow[i] = fields[i];
                        }

                        importedData.Rows.Add(importedRow);

                    }
                }
                
            }
            catch(Exception e)
            {
                Console.WriteLine("The file could not be read !!!");
                Console.WriteLine(e.Message);
            }
            return importedData;
        }

        private void SaveImportDataToDatabase(DataTable importData)
        {
            //string connectionString = "Server=SQLEXPRESS;Database = integrationTest;Trusted_Connection=True;";
            string connectionString = "Data Source=FADY-PC\\SQLEXPRESS;Initial Catalog=integrationTest;Integrated Security=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                foreach(DataRow importRow in importData.Rows)
                {
                    /*
                    SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth,NAM,NAMExpiryDate,Note,Mother_FirstName,Mother_LastName,Father_FirstName,Father_LastName,Email,Address,Country,ZipCode)"+
                                                    "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth,@NoAssMaladie,@Expiry_AM,@Note,@MotherFirstName,@MotherLastname,@FatherFirstName,@FatherLastname,@Email,@Address,@Country,@ZipCode )", 
                                                    conn);*/
                    
                    SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth)" +
                                                    "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth)",
                                                    conn);
                    //MessageBox.Show("==>"+importRow["FirstName"]+"");
                    cmd.Parameters.AddWithValue("@PatientGuid", importRow["PatientGuid"]);

                   // if (string.IsNullOrEmpty(importRow["FirstName"]))
                    /*
                    if (importRow["FirstName"] == null)
                    {
                        importRow["FirstName"] = "N/A";
                    }

                    if (importRow["LastName"] == null)
                    {
                        importRow["LastName"] = "N/A";
                    }
                    */
                    cmd.Parameters.AddWithValue("@FirstName", importRow["FirstName"]);
                    cmd.Parameters.AddWithValue("@LastName", importRow["LastName"]);

                    string inputString = importRow["Date Of Birth"].ToString();
              
                    DateTime dDate;
                    DateTime dob;
                   
         
                    if (DateTime.TryParse(inputString, out dDate))
                    {
                        dob = DateTime.Parse(inputString);
                    }
                    else
                    {
                        //Console.WriteLine("Invalid"); 
                        dob = DateTime.MinValue;
                    }
                    
                    //cmd.Parameters.AddWithValue("@Date_Of_Birth", importRow["Date Of Birth"]);
                    cmd.Parameters.AddWithValue("@Date_Of_Birth", dob);
                    
                    cmd.Parameters.AddWithValue("@NoAssMaladie", importRow["NoAssMaladie"]);
                    cmd.Parameters.AddWithValue("@Expiry_AM", importRow["Exp.year"] + "-" + importRow["Exp.Month"] +"-");
                    cmd.Parameters.AddWithValue("@Note", importRow["Note"]);

                    cmd.Parameters.AddWithValue("@MotherFirstName", importRow["MotherFirstName"]);
                    cmd.Parameters.AddWithValue("@MotherLastname", importRow["MotherLastname"]);
                    cmd.Parameters.AddWithValue("@FatherFirstName", importRow["FatherFirstName"]);
                    cmd.Parameters.AddWithValue("@FatherLastname", importRow["FatherLastname"]);
                    cmd.Parameters.AddWithValue("@Email", importRow["Email"]);
                    cmd.Parameters.AddWithValue("@Address", importRow["Address"]);
                    cmd.Parameters.AddWithValue("@Country", importRow["Country"]);
                    cmd.Parameters.AddWithValue("@ZipCode", importRow["ZipCode"]);

                    

                    ////////////////////

                    cmd.ExecuteNonQuery();


                }
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