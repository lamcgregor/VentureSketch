using System;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VentureSketch.Models;
using Dapper;

namespace VentureSketch.Data
{
    public class GenericRepository<T> : IRepository<T>, IDisposable
    {
        private IDbConnection _connection;
        private static PluralizationService _pluralisationService = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

        public GenericRepository(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public GenericRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public IEnumerable<T> List()
        {
            IEnumerable<T> allItems = null;

            string query = String.Format("SELECT * FROM {0}", _pluralisationService.Pluralize(typeof(T).Name));
            allItems = _connection.Query<T>(query).ToList();
            foreach (T item in allItems)
            {
                EagerLoadForeignKeyProperties(item);
            }
            return allItems;
        }

        public T Find(int id)
        {
            T result = default(T);

            string query = String.Format("SELECT * FROM {0} WHERE Id = {1}", _pluralisationService.Pluralize(typeof(T).Name), id);
            result = _connection.Query<T>(query).SingleOrDefault();
            if (result != null)
            {
                EagerLoadForeignKeyProperties(result);

                AssignXRefProperties(result);
            }
            return result;
        }

        public bool AddOrUpdate(T item)
        {
            bool success = false;

            if (item != null)
            {
                AssignForeignKeyProperties(item);

                int id;
                if (int.TryParse(typeof(T).GetProperty("Id").GetValue(item).ToString(), out id))
                {
                    PropertyInfo[] properties = item.GetType().GetProperties().Where(p => p.Name != "Id").ToArray();
                    if (id == 0)
                    {
                        string insertQuery = GetInsertQuery(item, properties);
                        id = _connection.Query<int>(insertQuery.ToString()).Single();
                        item.GetType().GetProperty("Id").SetValue(item, id);
                        success = true;
                    }
                    else
                    {
                        string updateQuery = GetUpdateQuery(item, properties);
                        int rowsAffected = _connection.Execute(updateQuery);
                        success = (rowsAffected == 1);
                    }
                }

                AssignXRefProperties(item);
            }

            return success;
        }

        public bool Delete(T item)
        {
            bool success = false;

            if (item != null)
            {
                int id = Convert.ToInt32(item.GetType().GetProperty("Id").GetValue(item));
                string deleteQuery = GetDeleteQuery(item);
                int rowsAffected = _connection.Execute(deleteQuery);
                success = (rowsAffected == 1);
            }

            return success;
        }

        #region IDisposable Methods
        public void Dispose()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }
        #endregion

        #region Private Methods
        private void EagerLoadForeignKeyProperties(T item)
        {
            PropertyInfo[] foreignKeyProperties = GetForeignKeyProperties(item);

            foreach (PropertyInfo property in foreignKeyProperties)
            {
                EagerLoad(item, property);
            }
        }
        
        private void AssignXRefProperties(object result)
        {
            PropertyInfo[] properties = GetXRefProperties(result);
            foreach (PropertyInfo property in properties)
            {
                string xRefTableName = property.Name;
                string xRefTableNameSingular = _pluralisationService.Singularize(xRefTableName);
                string query;
                IList o = (IList)property.GetValue(result);
                if (o != null)
                {
                    string xRefForeignKeyIdFieldName1 = String.Format("{0}Id", o[0].GetType().Name);
                    string xRefForeignKeyIdFieldName2 = String.Format("{0}Id", xRefTableNameSingular.Replace(o[0].GetType().Name, String.Empty));
                    string listItemTypeName = o[0].GetType().Name;
                    foreach (object listItem in o)
                    {
                        #region Add / Update Cross Ref Property
                        Type methodType = Type.GetType("VentureSketch.Data.GenericRepository`1");
                        Type[] typeArgs = new Type[] { listItem.GetType() };
                        Type constructedMethodType = methodType.MakeGenericType(typeArgs);
                        object classInstance = Activator.CreateInstance(constructedMethodType, new object[] { _connection });
                        MethodInfo method = constructedMethodType.GetMethod("AddOrUpdate");
                        method.Invoke(classInstance, new object[] { listItem });
                        result.GetType().GetProperty(property.Name).SetValue(result, property.GetValue(result));
                        ((IDisposable)classInstance).Dispose();
                        #endregion

                        int xRefIdValue1 = Convert.ToInt32(listItem.GetType().GetProperty("Id").GetValue(listItem));
                        int xRefIdValue2 = Convert.ToInt32(result.GetType().GetProperty("Id").GetValue(result));
                        query = GetXRefQuery(xRefTableName, xRefForeignKeyIdFieldName1, xRefForeignKeyIdFieldName2, xRefIdValue1, xRefIdValue2);
                        int rowsAffected = _connection.Execute(query);
                    }
                }
                query = String.Format("SELECT * FROM [{0}] WHERE [{1}Id]={2}", property.Name, result.GetType().Name, result.GetType().GetProperty("Id").GetValue(result));
                IEnumerable<dynamic> xRefProperties = _connection.Query<dynamic>(query);
                if (xRefProperties.Count() > 0)
                {
                    o = (o == null) ? (IList)Activator.CreateInstance(property.PropertyType) : o;
                    PopulateExistingXRefProperties(property, o, xRefProperties);
                }
                result.GetType().GetProperty(property.Name).SetValue(result, o);
            }
        }

        private void PopulateExistingXRefProperties(PropertyInfo property, IList o, IEnumerable<dynamic> xRefProperties)
        {
            Type elementType = o.GetType().GetGenericArguments().Single();

            int[] ids = new int[xRefProperties.Count()];
            int i = 0;
            foreach (dynamic d in xRefProperties)
            {
                foreach (KeyValuePair<string, object> kvp in d)
                {
                    if (kvp.Key == String.Format("{0}Id", elementType.Name))
                    {
                        ids[i++] = Convert.ToInt32(kvp.Value);
                        break;
                    }
                }
            }
            string subQuery = String.Format("SELECT * FROM [{0}] WHERE Id in ({1})", _pluralisationService.Pluralize(elementType.Name), String.Join(",", ids));
            IEnumerable<dynamic> subProperties = _connection.Query<dynamic>(subQuery);
            
            Populate(o, subProperties, elementType);
        }
        

        private void Populate(IList list, IEnumerable<dynamic> properties, Type listElementType)
        {
            foreach (dynamic property in properties)
            {
                object listElement = Activator.CreateInstance(listElementType);
                foreach (KeyValuePair<string, object> keyValuePair in property)
                {
                    listElement.GetType().GetProperty(keyValuePair.Key).SetValue(listElement, keyValuePair.Value);
                }
                list.Add(listElement);
            }
        }

        private PropertyInfo[] GetForeignKeyProperties(object item)
        {
            return typeof(T).GetProperties().Where(p => (p.Name.Length > 2) && (p.Name.Substring(p.Name.Length - 2, 2) == "Id")).ToArray();
        }

        private PropertyInfo[] GetXRefProperties(object item)
        {

            PropertyInfo[] crossRefProperties =
                typeof(T).GetProperties().
                Where(p =>
                    _pluralisationService.IsPlural(p.Name)
                    && (p.Name.Contains(typeof(T).Name) || p.Name.Contains(_pluralisationService.Pluralize(typeof(T).Name)))
                    && p.PropertyType.Name.Contains("List`1")).ToArray();

            return crossRefProperties;
        }
        
        private void AssignForeignKeyProperties(object item)
        {
            PropertyInfo[] foreignKeyProperties = GetForeignKeyProperties(item);
            foreach (PropertyInfo property in foreignKeyProperties)
            {
                string parentPropertyName = property.Name.Substring(0, property.Name.Length - 2);
                Type parentPropertyType = item.GetType().GetProperty(parentPropertyName).PropertyType;
                object o = item.GetType().GetProperty(parentPropertyName).GetValue(item);
                int subPropertyId = (int)(property.GetValue(item));
                if ((o == null) && (subPropertyId > 0))
                {
                    o = Activator.CreateInstance(parentPropertyType);
                    o.GetType().GetProperty("Id").SetValue(o, subPropertyId);
                    EagerLoad(item, property);
                }
                else if (o != null)
                {
                    subPropertyId = (int)o.GetType().GetProperty("Id").GetValue(o);
                    if (subPropertyId == 0)
                    {
                        string insertQuery = GetInsertQuery(o, o.GetType().GetProperties().Where(p => p.Name != "Id").ToArray());
                        subPropertyId = _connection.Query<int>(insertQuery).Single();
                        o.GetType().GetProperty("Id").SetValue(o, subPropertyId);
                        item.GetType().GetProperty(property.Name).SetValue(item, subPropertyId);
                    }
                    else
                    {
                        item.GetType().GetProperty(property.Name).SetValue(item, subPropertyId);
                        string updateQuery = GetUpdateQuery(o, o.GetType().GetProperties().Where(p => p.Name != "Id").ToArray());
                        int rowCount = _connection.Execute(updateQuery);
                    }
                }
            }
        }

        private void EagerLoad(object result, PropertyInfo property)
        {
            int propertyId = Convert.ToInt32(property.GetValue(result));
            string subPropertyName = property.Name.Substring(0, property.Name.Length - 2);
            Type subPropertyType = result.GetType().GetProperty(subPropertyName).PropertyType;
            object o = Convert.ChangeType(Activator.CreateInstance(subPropertyType), subPropertyType);

            Type methodType = Type.GetType("VentureSketch.Data.GenericRepository`1");
            Type[] typeArgs = new Type[] { subPropertyType };
            Type constructedMethodType = methodType.MakeGenericType(typeArgs);
            object classInstance = Activator.CreateInstance(constructedMethodType, new object[] { _connection });
            MethodInfo method = constructedMethodType.GetMethod("Find");
            o = method.Invoke(classInstance, new object[] { propertyId });
            result.GetType().GetProperty(subPropertyName).SetValue(result, o);
            ((IDisposable)classInstance).Dispose();
        }
        
        private string GetInsertQuery(object item, PropertyInfo[] properties)
        {
            StringBuilder queryInsertPart = null;
            StringBuilder queryValuePart = null;
            string result = null;

            if (properties.Length > 0)
            {
                queryInsertPart = new StringBuilder(String.Format("INSERT INTO {0} (", _pluralisationService.Pluralize(item.GetType().Name)));
                queryValuePart = new StringBuilder(String.Format("VALUES (", properties[properties.Length - 1].Name));
                
                for(int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    object propertyValue;
                    try
                    {
                        propertyValue = property.GetValue(item);
                    }
                    catch
                    {
                        propertyValue = null;
                    }
                    bool isPrimitiveProperty = (propertyValue != null) && propertyValue.GetType().IsPrimitive;
                    bool isStringProperty = (propertyValue as string) != null;

                    if (isPrimitiveProperty || isStringProperty)
                    {
                        queryInsertPart.Append(String.Format("[{0}]", property.Name));
                        queryValuePart.Append(String.Format("{0}{1}{2}", isStringProperty ? "'" : String.Empty, property.GetValue(item).ToString(), isStringProperty ? "'" : String.Empty));
                        queryInsertPart.Append(",");
                        queryValuePart.Append(",");
                    }
                    else
                    {
                        //int subPropertyId = Convert.ToInt32(item.GetType().GetProperty(String.Format("{0}Id")).GetValue(item));
                        //property.SetValue(property, Activator.CreateInstance(property.GetType()));
                        //string subInsertQuery = GetInsertQuery((T)Convert.ChangeType(propertyValue, typeof(T)), propertyValue.GetType().GetProperties());
                        
                    }
                    

                }
                queryInsertPart.Remove(queryInsertPart.Length - 1, 1).Append(") ");
                queryValuePart.Remove(queryValuePart.Length - 1, 1).Append("); ");

                queryValuePart.Append("SELECT CAST(SCOPE_IDENTITY() AS INT)");
                result = queryInsertPart.Append(queryValuePart).ToString();
            }
            return result;
        }

        private string GetUpdateQuery(object item, PropertyInfo[] properties)
        {
            StringBuilder query = null;
            string result = null;

            if (properties.Length > 0)
            {
                query = new StringBuilder(String.Format("UPDATE {0} SET ", _pluralisationService.Pluralize(item.GetType().Name)));
                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    object propertyValue;
                    try
                    {
                        propertyValue = property.GetValue(item);
                    }
                    catch
                    {
                        propertyValue = null;
                    }
                    bool isPrimitiveProperty = (propertyValue != null) && propertyValue.GetType().IsPrimitive;
                    bool isStringProperty = (propertyValue as string) != null;
                    if (isPrimitiveProperty || isStringProperty)
                    {
                        query.Append(String.Format("[{0}] = {1}{2}{3}", property.Name, isStringProperty ? "'" : String.Empty, propertyValue.ToString(), isStringProperty ? "'" : String.Empty));
                        if (i < properties.Length - 1)
                        {
                            query.Append(",");
                        } else {
                            query.Append(" ");
                        }
                    }
                }
                query.Remove(query.Length - 1, 1).Append(String.Format(" WHERE Id = {0}", item.GetType().GetProperty("Id").GetValue(item)));
                result = query.ToString();
            }

            return result;
        }

        private string GetDeleteQuery(T item)
        {
            StringBuilder deleteQuery = new StringBuilder(String.Format("DELETE FROM {0} ", _pluralisationService.Pluralize(typeof(T).Name)));
            deleteQuery.Append(String.Format("WHERE Id = {0}", item.GetType().GetProperty("Id").GetValue(item)));

            return deleteQuery.ToString();
        }

        private string GetXRefQuery(string xRefTableName, string xRefForeignKeyIdFieldName1, string xRefForeignKeyIdFieldName2, int xRefIdValue1, int xRefIdValue2) {
        string query = String.Format("IF NOT EXISTS (SELECT Id FROM [{0}] WHERE [{1}] = {2} AND [{3}] = {4}) "
                            + "BEGIN INSERT INTO [{5}] "
                            + "([{6}],[{7}]) VALUES ({8},{9}) END",
                            xRefTableName,
                            xRefForeignKeyIdFieldName1,
                            xRefIdValue1,
                            xRefForeignKeyIdFieldName2,
                            xRefIdValue2,
                            xRefTableName,
                            xRefForeignKeyIdFieldName1,
                            xRefForeignKeyIdFieldName2,
                            xRefIdValue1,
                            xRefIdValue2);
            return query;
        }
        #endregion
    }
}

