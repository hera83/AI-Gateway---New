using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGateway.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveVectorsToOwnDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // vec_chunks previously lived in the main AiGateway.db file, alongside the API-key and
            // knowledge-metadata tables. It now lives in its own App_dbs/AiGatewayVectors.db file,
            // attached under the "vectors" schema on every connection by
            // Data/SqliteVecConnectionInterceptor.cs — kept separate so heavy vector read/write
            // traffic can't slow down queries against the main database as the knowledge base grows.
            // Existing rows are copied over preserving rowid, since Data/Entities/KnowledgeChunk.cs's
            // VectorRowId column refers to it.
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
                SELECT rowid, embedding, api_key_id, group_id FROM vec_chunks;
                """);

            migrationBuilder.Sql("DROP TABLE vec_chunks;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vec_chunks USING vec0(
                    embedding float[768] distance_metric=cosine,
                    +api_key_id TEXT,
                    +group_id TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO vec_chunks(rowid, embedding, api_key_id, group_id)
                SELECT rowid, embedding, api_key_id, group_id FROM vectors.vec_chunks;
                """);

            migrationBuilder.Sql("DROP TABLE vectors.vec_chunks;");
        }
    }
}
