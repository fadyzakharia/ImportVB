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
                using (StreamReader sr = new StreamReader(txtFileName.Text))
                {
                    string header = sr.ReadLine();

                    if (string.IsNullOrEmpty(header))
                    {
                        MessageBox.Show("No file data !!!");
                        return null;
                    }

                    string[] headerColumns = header.Split(new string[] {"\";\""},StringSplitOptions.None);

                    //header.HasFieldsEnclosedInQuotes = true;
                    //HasFieldsEnclosedInQuotes = true;
                    //MessageBox.Show(headerColumns[0]);

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

                        string[] fields = line.Split(new string[] {"\";\""},StringSplitOptions.None);

                        //MessageBox.Show(fields[0]);

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
                        /*
                        SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth,NAM,NAMExpiryDate,Note,Mother_FirstName,Mother_LastName,Father_FirstName,Father_LastName,Email,Address,Country,ZipCode)"+
                                                        "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth,@NoAssMaladie,@Expiry_AM,@Note,@MotherFirstName,@MotherLastname,@FatherFirstName,@FatherLastname,@Email,@Address,@Country,@ZipCode )", 
                                                        conn);*/
                        SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth,NAM,NAMExpiryDate,Note,Mother_FirstName,Mother_LastName,Father_FirstName,Father_LastName,Email,Address,Country,ZipCode)" +
                                                        "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth,@NoAssMaladie,@Expiry_AM,@Note,@MotherFirstName,@MotherLastname,@FatherFirstName,@FatherLastname,@Email,@Address,@Country,@ZipCode )",
                                                        conn);
                        /*
                        SqlCommand cmd = new SqlCommand("insert into Patient(Id,FirstName,LastName,DateOfBirth,NAM,NAMExpiryDate,Note,Mother_FirstName,Mother_LastName,Father_FirstName,Father_LastName,Email,Address,Country,ZipCode)" +
                                                        "VALUES(@PatientGuid,@FirstName,@LastName,@Date_Of_Birth,@NoAssMaladie,@Expiry_AM,@Note,@MotherFirstName)",
                                                        conn);*/

                        //string str = " \"A\" ";
                        //MessageBox.Show(importRow[" \"PatientGuid\" "].ToString());

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
                                //Console.WriteLine(word);
                                ln = words[0].Replace(',', ' ');
                                fn = words[1].Replace(',', ' ');
                            }
                        }
                        else
                        {
                            //MessageBox.Show("2 !!!");
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
                            //Console.WriteLine("Invalid"); 
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

                        //cmd.Parameters.AddWithValue("@Date_Of_Birth", importRow["Date Of Birth"]);
                        //cmd.Parameters.AddWithValue("@Date_Of_Birth", dob);

                        cmd.Parameters.AddWithValue("@NoAssMaladie", importRow["NoAssMaladie"]);

                        if (importRow["NoAssMaladie"].ToString() == "")
                        {
                            cmd.Parameters.AddWithValue("@Expiry_AM", DBNull.Value);
                            //MessageBox.Show("ZAX1");
                        }
                        else
                        {
                            string expiry_am = importRow["Exp.year"] + "-" + importRow["Exp.Month"] + "-01";
                            DateTime expiry_am1 = DateTime.Parse(expiry_am);
                            cmd.Parameters.AddWithValue("@Expiry_AM", expiry_am1);
                            //MessageBox.Show("ZAX2");
                        }

                        //if (expiry_am1 < DateTime.Parse(System.Data.SqlTypes.SqlDateTime.MinValue.ToString()) || expiry_am1 > DateTime.Parse(System.Data.SqlTypes.SqlDateTime.MaxValue.ToString()))
                        //MessageBox.Show(importRow["Exp.year"].ToString());
                        //cmd.Parameters.AddWithValue("@Expiry_AM", importRow["Exp.year"] + "-" + importRow["Exp.Month"] + "-01");

                        cmd.Parameters.AddWithValue("@Note", importRow["Note"]);

                        cmd.Parameters.AddWithValue("@MotherFirstName", importRow["MotherFirstName"]);

                        cmd.Parameters.AddWithValue("@MotherLastname", importRow["MotherLastname"]);
                        cmd.Parameters.AddWithValue("@FatherFirstName", importRow["FatherFirstName"]);
                        cmd.Parameters.AddWithValue("@FatherLastname", importRow["FatherLastname"]);
                        cmd.Parameters.AddWithValue("@Email", importRow["Email"]);
                        cmd.Parameters.AddWithValue("@Address", importRow["Address"]);
                        cmd.Parameters.AddWithValue("@Country", importRow["Country"]);
                        cmd.Parameters.AddWithValue("@ZipCode", importRow["ZipCode"]);

                        cmd.ExecuteNonQuery();
                    }
                    // End Insert Table Patient
                    /**********************************************************************************/
                    /**********************************************************************************/
                    // Update table Patient
                    SqlDataReader dr;
                    string qr = "   SELECT distinct a.Id,g.fr as GenderLookup,l.fr as LanguageLookup,m.fr as MaritalStatusLookup,s.fr as StatusLookup "+
                                    "FROM Lookup as a "+
                                    "left join(select id,fr from Lookup where Type = 'Gender') as g on g.id = a.id "+
                                    "left join(select id,fr from Lookup where Type = 'LANGUAGE') as L on L.id = a.id "+
                                    "left join(select id,fr from Lookup where Type = 'MARITALSTATUS') as m on m.id = a.id "+ 
                                    "left join(select id,fr from Lookup where Type = 'STATUS') as s on s.id = a.id ";
                    SqlCommand cmd1 = new SqlCommand(qr,conn);
                    dr = cmd1.ExecuteReader();
                    if(dr.HasRows)
                    {
                        while(dr.Read())
                        {
                            //MessageBox.Show(dr["Id"].ToString());
                            string qr_upd = "update patient set GenderLookup = @GenderLookup,LanguageLookup = @LanguageLookup ,MaritalStatusLookup= @MaritalStatusLookup,StatusLookup = @StatusLookup where id = @id";
                            SqlCommand upd1 = new SqlCommand(qr_upd, conn);

                            upd1.Parameters.AddWithValue("@GenderLookup", dr["GenderLookup"].ToString());
                            upd1.Parameters.AddWithValue("@LanguageLookup", dr["LanguageLookup"].ToString());
                            upd1.Parameters.AddWithValue("@MaritalStatusLookup", dr["MaritalStatusLookup"].ToString());
                            upd1.Parameters.AddWithValue("@StatusLookup", dr["StatusLookup"].ToString());
                            upd1.Parameters.AddWithValue("@id", dr["Id"].ToString());
                            upd1.ExecuteNonQuery();
                            //MessageBox.Show("XXXX");
                        }
                    }
                    // End Update table Patient
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Data !!!");
                    Console.WriteLine(e.Message);
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