﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace translation_review_api
{
    public class ReviewData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id"), JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("Reviews")]
        public List<ReviewedQuestion> Reviews { get; set; } = new();
        [JsonPropertyName("LastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("LastReviewedIndex")]
        public int LastReviewedIndex { get; set; } = 0;
    }

    public class ReviewedQuestion
    {
        [BsonElement("_id"), JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("text")]
        public Dictionary<string, string> Text { get; set; } = new();
        [JsonPropertyName("options")]
        public List<Option> Options { get; set; } = new();
        [JsonPropertyName("explanation")]
        public Dictionary<string, string> Explanation { get; set; } = new();
    }

    public class Option
    {
        public Dictionary<string, string> Text { get; set; } = new();
    }
    public class MyDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id"), JsonPropertyName("_id")]
        public ObjectId Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Store hashed password in production
        public string Language { get; set; } = "all";
    }

    public class LoginRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
        [JsonPropertyName("selectedLanguage")]
        public string SelectedLanguage { get; set; } = string.Empty;
    }


}
