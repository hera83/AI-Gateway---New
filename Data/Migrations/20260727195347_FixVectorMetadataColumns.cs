using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGateway.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVectorMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // api_key_id/group_id were originally declared as vec0 "auxiliary" columns (the `+` prefix
            // in the AddKnowledgeBase migration). Auxiliary columns can be retrieved but sqlite-vec
            // rejects using them in a KNN query's WHERE clause ("An illegal WHERE constraint was
            // provided on a vec0 auxiliary column in a KNN query") — which is exactly what
            // KnowledgeBaseService.SearchAsync does to scope results to the caller's own data. Fixed by
            // redeclaring them as a "partition key" (api_key_id — present in every query, and vec0
            // physically shards storage by it, which is the documented use case for per-user/tenant
            // data) and a plain metadata column (group_id — only sometimes present in the WHERE clause,
            // so not a good partition key candidate), both of which support KNN pre-filtering.
            //
            // vec0 tables can't ALTER a column's declaration, and (verified separately, in isolation —
            // renaming a vec0 table silently breaks its shadow tables, e.g. "no such table:
            // vec_chunks_chunks" on the next query) ALTER TABLE RENAME is not safe to use on them
            // either. So this goes through a temporary holding table instead: copy out, drop, recreate
            // vec_chunks under its original name with the corrected schema, copy back in — preserving
            // rowid throughout, since KnowledgeChunk.VectorRowId depends on it.
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vectors.vec_chunks_migration_tmp USING vec0(
                    embedding float[768] distance_metric=cosine,
                    api_key_id TEXT partition key,
                    group_id TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO vectors.vec_chunks_migration_tmp(rowid, embedding, api_key_id, group_id)
                SELECT rowid, embedding, api_key_id, group_id FROM vectors.vec_chunks;
                """);

            migrationBuilder.Sql("DROP TABLE vectors.vec_chunks;");

            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vectors.vec_chunks USING vec0(
                    embedding float[768] distance_metric=cosine,
                    api_key_id TEXT partition key,
                    group_id TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO vectors.vec_chunks(rowid, embedding, api_key_id, group_id)
                SELECT rowid, embedding, api_key_id, group_id FROM vectors.vec_chunks_migration_tmp;
                """);

            migrationBuilder.Sql("DROP TABLE vectors.vec_chunks_migration_tmp;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vectors.vec_chunks_migration_tmp USING vec0(
                    embedding float[768] distance_metric=cosine,
                    +api_key_id TEXT,
                    +group_id TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO vectors.vec_chunks_migration_tmp(rowid, embedding, api_key_id, group_id)
                SELECT rowid, embedding, api_key_id, group_id FROM vectors.vec_chunks;
                """);

            migrationBuilder.Sql("DROP TABLE vectors.vec_chunks;");

            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vectors.vec_chunks USING vec0(
                    embedding float[768] distance_metric=cosine,
                    +api_key_id TEXT,
                    +group_id TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO vectors.vec_chunks(rowid, embedding, api_key_id, group_id)
                SELECT rowid, embedding, api_key_id, group_id FROM vectors.vec_chunks_migration_tmp;
                """);

            migrationBuilder.Sql("DROP TABLE vectors.vec_chunks_migration_tmp;");
        }
    }
}
