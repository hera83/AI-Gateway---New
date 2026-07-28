using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGateway.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeGroups_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    KnowledgeGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocuments_KnowledgeGroups_KnowledgeGroupId",
                        column: x => x.KnowledgeGroupId,
                        principalTable: "KnowledgeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    VectorRowId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_KnowledgeDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_ApiKeyId",
                table: "KnowledgeChunks",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_DocumentId",
                table: "KnowledgeChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_VectorRowId",
                table: "KnowledgeChunks",
                column: "VectorRowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_ApiKeyId",
                table: "KnowledgeDocuments",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_KnowledgeGroupId",
                table: "KnowledgeDocuments",
                column: "KnowledgeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeGroups_ApiKeyId_Name",
                table: "KnowledgeGroups",
                columns: new[] { "ApiKeyId", "Name" },
                unique: true);

            // vec0 is a virtual table provided by the sqlite-vec extension (loaded per-connection via
            // Data/SqliteVecConnectionInterceptor.cs) — EF Core has no concept of virtual tables, so this
            // is raw SQL. Dimension (768) matches appsettings.json's KnowledgeBase:EmbeddingDimensions for
            // the default nomic-embed-text model; changing the embedding model requires a fresh migration
            // and re-embedding all chunks, since vectors from different models aren't comparable.
            // api_key_id/group_id are "auxiliary" (+) columns: stored alongside the vector but excluded
            // from the similarity index itself, so a KNN query can pre-filter on ownership in one pass.
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE vec_chunks USING vec0(
                    embedding float[768] distance_metric=cosine,
                    +api_key_id TEXT,
                    +group_id TEXT
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS vec_chunks;");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropTable(
                name: "KnowledgeGroups");
        }
    }
}
