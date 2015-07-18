using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VentureSketch.Models;
using Dapper;

namespace VentureSketch.Data
{
    public class QualificationRepository : IDisposable
    {
        private IDbConnection _connection;

        public QualificationRepository(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public IEnumerable<Qualification> GetAll()
        {
            IEnumerable<Qualification> allQualifications = null;

            const string query = "SELECT * FROM Qualifications";
            allQualifications = _connection.Query<Qualification>(query).ToList();

            return allQualifications;
        }

        public Qualification GetById(int id)
        {
            Qualification qualification = null;

            string query = String.Format("SELECT * FROM Qualifications WHERE id={0}", id);
            qualification = _connection.Query<Qualification>(query).SingleOrDefault();

            return qualification;
        }

        public void Dispose()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

    }
}
