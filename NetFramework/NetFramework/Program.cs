using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using System.Reflection;

namespace NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("================ Attribute ================\n");
            Atrributes();
            TestAtrributeReader();
            Console.WriteLine("================ Attribute ================\n");

            Console.WriteLine("============ DBProviderFactory =============\n");
            DBProviderFactory();
            Console.WriteLine("============ DBProviderFactory =============\n");

            Console.WriteLine("======== IDBConnectionICommandIReader =======\n");
            IDBConnectionICommandIReader();
            Console.WriteLine("======== IDBConnectionICommandIReader =======\n");
        }

        public class MyClass
        {

            [Obsolete("Phương thức này lỗi thời, hãy dùng phương thức Method2")]
            public static void Method1()
            {
                Console.WriteLine("Method1");
            }
        }

        public static void Atrributes()
        {
            MyClass.Method1();
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
        public class DescriptionAttribute : Attribute // có thể đặt tên Description thay cho DescriptionAttribute
        {
            // Phương thức khởi tạo
            public DescriptionAttribute(string description) => Description = description;

            public string Description { set; get; }
        }

   
        public static void TestAtrributeReader()
        {
            var test = new Test();

            // Đọc các Attribute của lớp
            foreach (Attribute attr in test.GetType().GetCustomAttributes(false))
            {
                DescriptionAttribute description = attr as DescriptionAttribute;
                if (attr != null)
                {
                    Console.WriteLine($"{test.GetType().Name} : {description.Description}");
                }
            }

            // Đọc Attribute của từng thuộc tính lớp
            foreach (var property in test.GetType().GetProperties())
            {
                foreach (Attribute attr in property.GetCustomAttributes(false))
                {
                    DescriptionAttribute description = attr as DescriptionAttribute;
                    if (description != null)
                    {
                        Console.WriteLine($"{attr.GetType().Name} : {description.Description}");
                    }
                }
            }

            // Đọc Attribute của phương thức
            foreach (var method in test.GetType().GetMethods())
            {
                foreach (Attribute attr in method.GetCustomAttributes(false))
                {
                    DescriptionAttribute mota = attr as DescriptionAttribute;
                    if (mota != null)
                    {
                        Console.WriteLine($"{method.Name} : {mota.Description}");
                    }
                }
            }
        }

        // More information https://www.freecodespot.com/blog/viewing-sql-database-connection/
        public const string CONNECTION_STRING = @"Data Source=DESKTOP-HRO9T40\SQLEXPRESS;Initial Catalog=Test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        [Description("class mapping table test in db")]
        public class Test
        {
            [Description("Mapping ID")]
            public int ID { get; set; }
            [Description("Mapping Name")]
            public string Name { get; set; }
            [Description("Mapping Code")]
            public string Code { get; set; }

            [Description("Method print info")]
            public void PrintInfo()
            {
                Console.WriteLine("================");
                Console.WriteLine("ID: " + this.ID);
                Console.WriteLine("Name: " + this.Name);
                Console.WriteLine("Code: " + this.Code);
                Console.WriteLine("================");
            }
        }

        public interface IEntityMapper
        {
            object Map(IDataRecord record);
        }

        public class TestMapper : IEntityMapper
        {
            public object Map(IDataRecord record)
            {
                var test = new Test();

                test.ID = (int)record["ID"];
                test.Name = record["name"].ToString();
                test.Code = record["code"].ToString();

                return test;
            }
        }

        public static void IDBConnectionICommandIReader()
        {
            var mapper = new TestMapper();

            // More information https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/data-providers
            DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = CONNECTION_STRING;
                connection.Open();

                // Create command.
                DbCommand baseCmd = connection.CreateCommand(); 
                baseCmd.CommandText = "SELECT * FROM test";

                using (var reader = baseCmd.ExecuteReader()) {
                    while (reader.Read())
                    {
                        Test test = (Test)mapper.Map(reader);

                        test.PrintInfo();
                    }
                }

            }
        }

        public static void DBProviderFactory()
        {
            /*
            Table Dùng để test
               CREATE TABLE [dbo].[test] (
               [id]   INT IDENTITY (1, 1) NOT NULL,
               [name] NVARCHAR (255) NULL,
               [code] NVARCHAR (255) NULL,
               PRIMARY KEY CLUSTERED ([id] ASC)
            );
            */

            // More information https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/data-providers
            DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = CONNECTION_STRING;

                // Create command.
                DbCommand baseCmd = connection.CreateCommand();
                DbCommand usedCmd = connection.CreateCommand();

                usedCmd.Connection = connection;

                // Create adapter.
                DbDataAdapter adapter = factory.CreateDataAdapter();

                Console.WriteLine("================ SELECT ================");
                adapter.SelectCommand = baseCmd;
                baseCmd.CommandText = "SELECT * FROM test";

                // Fill the DataTable.
                DataTable table = new DataTable();
                adapter.Fill(table);

                PrintTable(table);
                Console.WriteLine("============== END SELECT ==============");

                Console.WriteLine("================ INSERT ================");
                // Hard with parameter
                adapter.InsertCommand = usedCmd;
                usedCmd.CommandText = @"INSERT INTO [test]([name], [code]) values(@name, @code)";

                DbParameter nameParam = usedCmd.CreateParameter();
                nameParam.ParameterName = "@name";
                nameParam.DbType = DbType.String;
                nameParam.SourceColumn = "name";

                DbParameter codeParam = usedCmd.CreateParameter();
                codeParam.ParameterName = "@code";
                codeParam.DbType = DbType.String;
                codeParam.SourceColumn = "code";

                usedCmd.Parameters.Add(nameParam);
                usedCmd.Parameters.Add(codeParam);

                DataRow newRow2 = table.NewRow();
                newRow2["name"] = "hard insert";
                newRow2["code"] = "hard insert";
    
                table.Rows.Add(newRow2);
                adapter.Update(table);

                // Using builder
                DbCommandBuilder builder = factory.CreateCommandBuilder();
                builder.DataAdapter = adapter;
                adapter.InsertCommand = builder.GetInsertCommand(true);

                DataRow newRow1 = table.NewRow();
                newRow1["name"] = "using builder insert";
                newRow1["code"] = "using builder insert";

                table.Rows.Add(newRow1);
                adapter.Update(table);

                table = new DataTable();
                adapter.Fill(table);

                PrintTable(table);
                Console.WriteLine("============== END INSERT ==============");

                Console.WriteLine("================ UPDATE ================");
                DataRow[] editRows = table.Select("name = 'using builder insert'");
                Console.WriteLine("---> Update code => code is updated");
                foreach (DataRow row in editRows)
                {
                    row["code"] = "code is updated";
                }

                adapter.Update(table);

                table = new DataTable();
                adapter.Fill(table);

                PrintTable(table);
                Console.WriteLine("============== END UPDATE ==============");

                Console.WriteLine("================ DELETE ================");
                DataRow[] deleteRows = table.Select("name = 'using builder insert'");
                Console.WriteLine("---> delete record has name = using builder insert");
                foreach (DataRow row in deleteRows)
                {
                    row.Delete();
                }
                adapter.Update(table);

                table = new DataTable();
                adapter.Fill(table);

                PrintTable(table);
                Console.WriteLine("============== END DELETE ==============");
            }
        }

        public static void PrintTable(DataTable table)
        {
            var rows = table.Rows;
            var cols = table.Columns;

            for (int i = 0; i < rows.Count; i++)
            {
                Console.WriteLine("row: " + i);
                for (int j = 0; j < cols.Count; j++)
                {
                    Console.WriteLine(cols[j].ColumnName + ": " + rows[i].ItemArray[j]);
                }
                Console.WriteLine();
            }
        }
    }
}


