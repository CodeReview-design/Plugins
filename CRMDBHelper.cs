using Croatia.Integration.BLL.Database;
using Croatia.Integration.Contracts.Common;
using Croatia.Integration.EntitiesEarlyBoundClasses;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Croatia.Integration.BLL.Common
{

    public class CRMDBHelper
    {
        public static readonly string connectionString = ConfigurationManager.ConnectionStrings["CRM_DB_Connection"]?.ConnectionString;
        public class Filter
        {
            public List<Condition> Conditions { get; set; }
            public List<JoinCondition> Joins { get; set; }
            public OperatorEnum Operator { get; set; } = OperatorEnum.and;

        }
        public class Criteria
        {
            public List<Filter> Filters { get; set; }
            public OperatorEnum Operator { get; set; } = OperatorEnum.and;

        }
        public class Condition
        {
            public string Scheme { get; set; }
            public string Value { get; set; }
            public string ComparisonOperator { get; set; } = "=";
        }
        public class JoinCondition
        {
            /// <summary>
            /// inner, left, right
            /// </summary>
            public string JoinType { get; set; }
            public string RelatedEntityName { get; set; }
            public string ParentEntityName { get; set; }

            public string ParentScheme { get; set; }
            public string RelatedScheme { get; set; }
            public List<string> ExtraConditions { get; set; }
        }

        public enum OperatorEnum
        {
            and = 1,
            or = 2
        }

        public enum OrderType
        {
            desc = 1,
            asc = 2
        }
        public class OrderBy
        {
            public OrderType OrderType { get; set; }
            public string orderBySchema { get; set; }
        }
        public static Entity GetActiveEntityBy(string entityName, string scheme, string value, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = entityName + "id, " + string.Join(", ", retrievedData);
            string query = "SELECT " + columns + " FROM " + entityName + "Base with (nolock) WHERE statecode = '0' and " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }

            return DataTableToEntityMapper.MapToEntity(dataTable, entityName);
        }
        public static Entity GetEntityBy(string entityName, string scheme, string value, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = entityName + "id, " + string.Join(", ", retrievedData);
            string query = "SELECT " + columns + " FROM " + entityName + "Base with (nolock) WHERE " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }

            return DataTableToEntityMapper.MapToEntity(dataTable, entityName);
        }
        public static Entity GetSystemSettingsBy(string entityName, string scheme, string value, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = string.Join(", ", retrievedData);
            string query = "SELECT " + columns + " FROM " + entityName + "Base with (nolock) WHERE " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }

            return DataTableToEntityMapper.MapToEntity(dataTable, entityName);
        }
        public static DataCollection<Entity> GetEntitiesBy(string entityName, string scheme, string value)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = "SELECT * FROM " + entityName + "Base with (nolock) WHERE statecode = '0' and " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, entityName);
        }
        /// <summary>
        /// This function Use The same manar SQL use and return DataCollection
        /// </summary>
        /// <param name="queryBuilder"></param>
        /// <returns>DataCollection</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static DataCollection<Entity> GetEntitiesBy(QueryBuilder queryBuilder)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = queryBuilder.Build();
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, queryBuilder._entityName);
        }

        public static DataCollection<Entity> GetEntitiesBy(string entityName, List<Condition> conditions, OperatorEnum op, int pageSize = 0, int pageNumber = 0, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = entityName + "id, " + string.Join(", ", retrievedData);
            //string query = @"SELECT * FROM " + entityName + @"Base with (nolock) ";
            string query = "SELECT " + columns + " FROM " + entityName + "Base with (nolock)";
            if (conditions.Count > 0) query += "WHERE ";
            for (int i = 0; i < conditions.Count; i++)
            {
                query += conditions[i].Scheme + " " + conditions[i].ComparisonOperator + " '" + conditions[i].Value + "'";
                if (conditions.Count - i > 1) query += " " + ((OperatorEnum)op).ToString() + " ";
            }
            if (pageNumber > 0 && pageSize > 0)
                query += @" ORDER BY CreatedOn OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, entityName);
        }
        /// <summary>
        /// get entities using conditions and joins
        /// </summary>
        /// <param name="entityName">parent entity name</param>
        /// <param name="conditions"></param>
        /// <param name="joins">related entities (now support multi join only)</param>
        /// <param name="op">and, or</param>
        /// <param name="orderBy"> entity scheme name to order by</param>
        /// <param name="orderType"> asc or desc</param>
        /// <param name="retrievedData"> column set to be retreived PR. for parent columns </param>
        /// <returns></returns>
        public static DataCollection<Entity> GetEntitiesBy(string entityName, Criteria criteria, string orderBy, int? pageSize,
            OrderType orderType = OrderType.desc, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = "PR." + entityName + "id, " + string.Join(", ", retrievedData);
            string query = @"SELECT ";
            if (pageSize != null && pageSize > 0)
                query += $"Top({pageSize}) ";
            query += columns + " FROM " + entityName + @"Base as PR with (nolock) ";

            if (criteria != null && criteria.Filters != null)
            {
                foreach (Filter filter in criteria.Filters)
                {
                    if (filter.Joins != null)
                    {
                        foreach (var join in filter.Joins)
                        {
                            var ParentAlias = join.ParentEntityName == null ? "PR" : join.ParentEntityName + "base";
                            query += $" {join.JoinType} join {join.RelatedEntityName}base on {ParentAlias}.{join.ParentScheme} = {join.RelatedEntityName}base.{join.RelatedScheme}";

                        }
                    }
                }
                bool initConditions = false;
                foreach (Filter filter in criteria.Filters)
                {
                    if (filter.Conditions != null)
                    {
                        if (!initConditions && filter.Conditions.Count > 0)
                        {
                            query += " WHERE (";
                            initConditions = true;
                        }
                        else if (initConditions && filter.Conditions.Count > 0) query += $" {criteria.Operator} (";

                        for (int i = 0; i < filter.Conditions.Count; i++)
                        {
                            query += filter.Conditions[i].Scheme + " " + filter.Conditions[i].ComparisonOperator + " '" + filter.Conditions[i].Value + "'";
                            if (filter.Conditions.Count - i > 1) query += " " + filter.Operator.ToString() + " ";
                            else query += ")";
                        }
                    }
                }
            }
            if (orderBy != null)
                query += " order by " + orderBy + " " + orderType.ToString() + ";";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, entityName);
        }

        public static DataCollection<Entity> GetOrderedEntitiesBy(string entityName, Criteria criteria, List<OrderBy> orderBy, int? pageSize, params string[] retrievedData)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string columns;
            if (retrievedData == null || retrievedData.Length <= 0) columns = "*";
            else columns = "PR." + entityName + "id, " + string.Join(", ", retrievedData);
            string query = @"SELECT ";
            if (pageSize != null && pageSize > 0)
                query += $"Top({pageSize}) ";
            query += columns + " FROM " + entityName + @"Base as PR with (nolock) ";

            if (criteria != null && criteria.Filters != null)
            {
                foreach (Filter filter in criteria.Filters)
                {
                    if (filter.Joins != null)
                    {
                        foreach (var join in filter.Joins)
                        {
                            var parentAlias = join.ParentEntityName == null ? "PR" : join.ParentEntityName + "base";
                            var onClause = $"{parentAlias}.{join.ParentScheme} = {join.RelatedEntityName}base.{join.RelatedScheme}";
                            if (join.ExtraConditions != null && join.ExtraConditions.Any())
                            {
                                onClause += " AND " + string.Join(" AND ", join.ExtraConditions);
                            }
                            query += $" {join.JoinType} JOIN {join.RelatedEntityName}base with (nolock) ON {onClause}";
                        }

                    }
                }
                bool initConditions = false;
                foreach (Filter filter in criteria.Filters)
                {
                    if (filter.Conditions != null)
                    {
                        if (!initConditions && filter.Conditions.Count > 0)
                        {
                            query += " WHERE (";
                            initConditions = true;
                        }
                        else if (initConditions && filter.Conditions.Count > 0) query += $" {criteria.Operator} (";

                        for (int i = 0; i < filter.Conditions.Count; i++)
                        {
                            var value = filter.Conditions[i].Value.ToLower().Contains("null") ? filter.Conditions[i].Value : " '" + filter.Conditions[i].Value + "'";
                            query += filter.Conditions[i].Scheme + " " + filter.Conditions[i].ComparisonOperator + value;
                            if (filter.Conditions.Count - i > 1) query += " " + filter.Operator.ToString() + " ";
                            else query += ")";
                        }
                    }
                }
            }
            if (orderBy != null && orderBy.Any())
            {
                var orderBySql = string.Join(", ",
                    orderBy.Select(o => $"{o.orderBySchema} {o.OrderType.ToString()}"));

                query += $" order by {orderBySql}";
            }
            query += ";";

            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {

                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, entityName);
        }
        /// <summary>
        ///  get entity id where status is active
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="value"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Entity GetEntityIdBy(string entityName, string value, string scheme = "cro_code")
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = "SELECT " + entityName + "id FROM " + entityName + "Base with (nolock) WHERE statecode = '0' and " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntity(dataTable, entityName);
        }
        public static List<Guid> GetEntityIdsBy(string entityName, List<Condition> conditions, OperatorEnum op)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = @"SELECT " + entityName + "id FROM " + entityName + @"Base with (nolock) ";
            if (conditions.Count > 0) query += "WHERE ";
            for (int i = 0; i < conditions.Count; i++)
            {
                query += conditions[i].Scheme + " " + conditions[i].ComparisonOperator + " '" + conditions[i].Value + "'";
                if (conditions.Count - i > 1) query += " " + ((OperatorEnum)op) + " ";
            }
            using (DataTable dataTable = new DataTable())
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        try
                        {
                            connection.Open();
                            using (SqlDataReader reader = command.ExecuteReader())
                                dataTable.Load(reader);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error fetching customer data: {ex.Message}");
                            throw;
                        }
                    }
                }
                return DataTableToEntityMapper.MapToListOfIds(dataTable, entityName);
            }
        }

        public static DataTable FetchEntityDataUsingSQL(string mainEntityName, List<string> selectColumns, string mainEntityAlais = null,
                                                               List<EntityRelationshipMapping> entityRelationships = null, List<QueryCondition> conditions = null,
                                                                  PaginationAndSortingOptions paginationAndSortingOptions = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string joinType = string.Empty;
            string selectColumnsString = string.Join(", ", selectColumns.Select(column => $"{column} AS [{column}]"));
            string query = $"SELECT DISTINCT {selectColumnsString} FROM {mainEntityName} AS {mainEntityAlais} with (nolock) ";

            if (entityRelationships != null && entityRelationships.Count > 0)
            {
                //             RelatedEntityName   RelatedEntityAlais    PrimaryEntityAlais   RelationshipLookupField  RelatedEntityAlais   RelatedEntityKey
                // LEFT JOIN   cro_city            LOC               ON  GV                   .cro_location  =         LOC                  .cro_cityid
                foreach (var relation in entityRelationships)
                {
                    //query += $" LEFT  JOIN {relation.RelatedEntityName} {relation.RelatedEntityAlais} " +
                    //         $"ON {relation.PrimaryEntityAlais}.{relation.RelationshipLookupField} = {relation.RelatedEntityAlais}.{relation.RelatedEntityKey}";

                    // joinType = Convert.ToString(relation.JoinType).Trim() == string.Empty ? JoinType.LeftJoin : relation.JoinType;
                    joinType = string.IsNullOrEmpty(relation.JoinType) == true ? JoinType.LeftJoin : relation.JoinType;
                    query += $" {joinType} {relation.RelatedEntityName} {relation.RelatedEntityAlais} " +
         $"ON {relation.PrimaryEntityAlais}.{relation.RelationshipLookupField} = {relation.RelatedEntityAlais}.{relation.RelatedEntityKey}";

                    //query += $" LEFT  JOIN {relation.RelatedEntityName} {relation.RelatedEntityAlais} " +
                    //         $"ON {relation.PrimaryEntityAlais}.{relation.RelationshipLookupField} = {relation.RelatedEntityAlais}.{relation.RelatedEntityKey}";

                }
            }
            if (conditions != null && conditions.Count > 0)
            {
                var conditionStrings = string.Join(" AND ", conditions.Select(c =>
                {
                    if (c.ColumnName == "(GroupedCondition)")
                    {
                        // Special case for grouped conditions
                        return c.Value.ToString();
                    }
                    else if (c.Operator == "IS NOT NULL" || c.Operator == "IS NULL")
                    {
                        return $"{c.ColumnName} {c.Operator}";
                    }
                    else if (c.Operator == "IN" || c.Operator == ">=" || c.Operator == "<=")
                    {
                        // no quotes around the value
                        return $"{c.ColumnName} {c.Operator} {c.Value}";
                    }
                    else if (Convert.ToString(c.Value).ToUpper() == "GETDATE()")
                    {
                        return $"{c.ColumnName} {c.Operator} {c.Value}";
                    }
                    else
                    {
                        return $"{c.ColumnName} {c.Operator} '{c.Value}'";
                    }
                }));
                query += $" WHERE {conditionStrings}";
            }
            if (paginationAndSortingOptions != null)
            {
                if (!string.IsNullOrEmpty(paginationAndSortingOptions.SortBy))
                {
                    var orderBy = paginationAndSortingOptions.OrderBy ?? "ASC"; // Use the specified order value or default to "ASC" if null.
                    query += $" ORDER BY {paginationAndSortingOptions.SortBy} {orderBy}"; //ORDER BY GV.cro_groupofvouchername ASC
                }

                if (paginationAndSortingOptions.PageNumber.HasValue && paginationAndSortingOptions.PageSize.HasValue)
                {
                    int offset = (paginationAndSortingOptions.PageNumber.Value - 1) * paginationAndSortingOptions.PageSize.Value;
                    query += $" OFFSET {offset} ROWS FETCH NEXT {paginationAndSortingOptions.PageSize.Value} ROWS ONLY";
                }
            }
            // fetch the results into a data table.
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching data: {ex.Message}");
                        throw;
                    }
                }
            }
            return dataTable;
        }

        public class QueryCondition
        {
            public string ColumnName { get; set; }
            public object Value { get; set; }
            public string Operator { get; set; } = "=";
        }
        public class EntityRelationshipMapping
        {
            public string RelatedEntityName { get; set; }
            public string RelatedEntityAlais { get; set; }
            public string PrimaryEntityAlais { get; set; }
            public string RelationshipLookupField { get; set; }
            public string RelatedEntityKey { get; set; }
            public string JoinType { get; set; }
        }
        public class JoinType
        {
            public static string InnerJoin = "INNER JOIN";
            public static string LeftJoin = "LEFT JOIN";
            public static string RightJoin = "RIGHT JOIN";
            public static string FullJoin = "FULL OUTER JOIN";
        }

        public class PaginationAndSortingOptions
        {
            public string SortBy { get; set; }
            public string OrderBy { get; set; }
            public int? PageNumber { get; set; }
            public int? PageSize { get; set; }
        }

        public static DataCollection<Entity> GetRelatedEntitiesByLookup(string tableName, string lookupFieldName, string parentId, List<string> selectFields = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");

            string columns = "*";
            if (selectFields != null && selectFields.Count > 0)
            {
                // Include primary ID by default
                if (!selectFields.Contains($"{tableName}id"))
                    selectFields.Insert(0, $"{tableName}id");

                columns = string.Join(", ", selectFields);
            }

            string query = $"SELECT {columns} FROM {tableName} WITH (NOLOCK) " +
                           $"WHERE {lookupFieldName} = @ParentId";

            DataTable dataTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ParentId", parentId);
                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing query: {ex.Message}");
                    throw;
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, tableName);
        }

        public static DataCollection<Entity> GetDataOf(string entityName)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = "SELECT * FROM " + entityName + "Base with (nolock) WHERE statecode = '0'";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }
            return DataTableToEntityMapper.MapToEntityCollection(dataTable, entityName);
        }

        #region contact fetches
        public static Entity GetContactAfterCreationBy(string entityName, string scheme, string value)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = "SELECT contactid, modifiedon, fullname, cro_customerid, cro_referralcode FROM " + entityName + "Base with (nolock) WHERE statecode = '0' and " + scheme + " = @value";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                            dataTable.Load(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching customer data: {ex.Message}");
                        throw;
                    }
                }
            }

            return DataTableToEntityMapper.MapToEntity(dataTable, entityName);
        }
        public static List<Guid> GetActive_ProspectContactIdBy(string entityName, List<Condition> conditions, OperatorEnum op)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not configured.");
            string query = @"SELECT " + entityName + "id FROM " + entityName + @"Base with (nolock) ";
            if (conditions.Count > 0) query += "WHERE ";
            for (int i = 0; i < conditions.Count; i++)
            {
                query += conditions[i].Scheme + " = '" + conditions[i].Value + "'";
                if (conditions.Count - i > 1) query += " " + ((OperatorEnum)op) + " ";
            }
            query += " and ( statuscode = '171080000' or statuscode = '1') ";
            using (DataTable dataTable = new DataTable())
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        try
                        {
                            connection.Open();
                            using (SqlDataReader reader = command.ExecuteReader())
                                dataTable.Load(reader);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error fetching customer data: {ex.Message}");
                            throw;
                        }
                    }
                }
                return DataTableToEntityMapper.MapToListOfIds(dataTable, entityName);
            }
        }
        #endregion

        public static GenericResponse HandleErrorMessage(string key)
        {
            GenericResponse genericResponse = new GenericResponse { Code = HttpStatusCode.InternalServerError, Message = "Something wrong happen while retrieving Error Template from DB for " + key + "." };
            Entity errorConfiguration = GetActiveEntityBy(IntegrationErrorMessage.EntityLogicalName, IntegrationErrorMessage.Fields.Name, key);
            if (errorConfiguration != null)
            {
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.HttpStatusCode))
                {
                    int code = Convert.ToInt32(errorConfiguration[IntegrationErrorMessage.Fields.HttpStatusCode].ToString());
                    genericResponse.Code = (HttpStatusCode)code;
                }
                else
                {
                    genericResponse.Code = HttpStatusCode.InternalServerError;
                }
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.Message))
                {
                    genericResponse.Message = errorConfiguration[IntegrationErrorMessage.Fields.Message].ToString();

                }
                else
                {
                    genericResponse.Message = key;
                }
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.ArabicMessage))
                {
                    genericResponse.MessageAr = errorConfiguration["cro_arabicmessage"].ToString();
                }
                else
                {
                    genericResponse.MessageAr = key;
                }
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.ErrorCode))
                {
                    genericResponse.ErrorCode = errorConfiguration[IntegrationErrorMessage.Fields.ErrorCode].ToString();
                }
                else
                {
                    genericResponse.ErrorCode = "-1001";
                }

            }
            return genericResponse;
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        #region Logging Process
        public static void LogInboundAsync(string functionName, object request, DateTime requestTime, string userID, object response, string code, string message, string customerId, Dictionary<string, string> queryParmeters = null)
        {
            var RequestId = HttpContext.Current != null ? HttpContext.Current.Request.Headers.Get("reqid") : "";
            var header = GetRequestHeaders() ?? "";
            Task.Run(() =>
            {
                using (Croatia_Integration_logEntities1 integDb = new Croatia_Integration_logEntities1())
                {

                    CRO_ONLINE_LOG log = new CRO_ONLINE_LOG();
                    log.RequestXML = request != null ? JsonConvert.SerializeObject(request) : "";
                    log.CIC = customerId ?? "";
                    log.MessageID = GetLocalIPAddress();
                    log.UserID = userID ?? "";
                    log.FunctionName = functionName ?? "";
                    log.RequestTimestamp = requestTime;
                    log.RequestId = RequestId;
                    log.Headers = header;
                    log.QueryParameters = GetRequestQueryParams(queryParmeters != null ? queryParmeters : new Dictionary<string, string>());
                    log.ResponseXML = JsonConvert.SerializeObject(response) ?? "";
                    log.ReturnCode = code ?? "";
                    log.ReturnMessage = message ?? "";
                    log.ResponseTimestamp = DateTime.Now;
                    integDb.CRO_ONLINE_LOG.Add(log);
                    integDb.SaveChanges();
                }
            });
        }
        private static string GetRequestHeaders()
        {
            if (HttpContext.Current == null) return "{}";
            var headers = new Dictionary<string, string>();
            foreach (var key in HttpContext.Current.Request.Headers.AllKeys)
                headers[key] = HttpContext.Current.Request.Headers[key];
            return JsonConvert.SerializeObject(headers, Formatting.Indented);
        }
        private static string GetRequestQueryParams(Dictionary<string, string> queryParmeters)
        {
            if (queryParmeters == null || queryParmeters.Count <= 0) return "{}";
            return JsonConvert.SerializeObject(queryParmeters, Formatting.Indented);
        }

        public static string getNameOf(string entityLogicalName, string primaryId, string nameSchema)
        {
            Entity entity = CRMDBHelper.GetEntityBy(entityLogicalName,
                                   entityLogicalName+"id", primaryId, nameSchema);
            return entity.Contains(nameSchema) ? entity[nameSchema].ToString() : null;
        }


        #endregion

    }


}

