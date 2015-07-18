using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VentureSketch.Models;

namespace VentureSketch.Data
{
    public interface IRepository<T> :IDisposable
    {
        IEnumerable<T> List();
        T Find(int Id);
        bool AddOrUpdate(T entity);
        bool Delete(T entity);
    }
}
