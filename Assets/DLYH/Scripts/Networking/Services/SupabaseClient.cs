// SupabaseClient.cs
// HTTP client wrapper for Supabase REST API
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// HTTP client for Supabase REST API using UnityWebRequest.
    /// Handles authentication headers, JSON serialization, and error handling.
    /// </summary>
    public class SupabaseClient
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly SupabaseConfig _config;
        private string _accessToken; // For authenticated requests (optional)

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public SupabaseClient(SupabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.IsValid)
            {
                Debug.LogError($"[SupabaseClient] Invalid config: {_config.ValidationError}");
            }
        }

        // ============================================================
        // AUTHENTICATION
        // ============================================================

        /// <summary>
        /// Set the access token for authenticated requests.
        /// Call this after user signs in.
        /// </summary>
        public void SetAccessToken(string token)
        {
            _accessToken = token;
        }

        /// <summary>
        /// Clear the access token (on sign out).
        /// </summary>
        public void ClearAccessToken()
        {
            _accessToken = null;
        }

        /// <summary>
        /// Whether the client has an access token set.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

        // ============================================================
        // HTTP METHODS
        // ============================================================

        /// <summary>
        /// Perform a GET request to the REST API.
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="query">Query parameters (e.g., "id=eq.ABC123")</param>
        /// <returns>Response body as string, or null on error</returns>
        public async UniTask<SupabaseResponse> Get(string table, string query = null)
        {
            string url = BuildUrl(table, query);
            return await SendRequest(url, "GET", null);
        }

        /// <summary>
        /// Perform a POST request to insert a new row.
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="json">JSON body to insert</param>
        /// <param name="returnRepresentation">If true, returns the inserted row</param>
        /// <returns>Response</returns>
        public async UniTask<SupabaseResponse> Post(string table, string json, bool returnRepresentation = true)
        {
            string url = BuildUrl(table);
            var headers = new Dictionary<string, string>();
            if (returnRepresentation)
            {
                headers["Prefer"] = "return=representation";
            }
            return await SendRequest(url, "POST", json, headers);
        }

        /// <summary>
        /// Perform a PATCH request to update existing rows.
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="query">Query to select rows to update (e.g., "id=eq.ABC123")</param>
        /// <param name="json">JSON body with fields to update</param>
        /// <param name="returnRepresentation">If true, returns the updated row(s)</param>
        /// <returns>Response</returns>
        public async UniTask<SupabaseResponse> Patch(string table, string query, string json, bool returnRepresentation = true)
        {
            string url = BuildUrl(table, query);
            var headers = new Dictionary<string, string>();
            if (returnRepresentation)
            {
                headers["Prefer"] = "return=representation";
            }
            return await SendRequest(url, "PATCH", json, headers);
        }

        /// <summary>
        /// Perform a DELETE request to delete rows.
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="query">Query to select rows to delete</param>
        /// <returns>Response</returns>
        public async UniTask<SupabaseResponse> Delete(string table, string query)
        {
            string url = BuildUrl(table, query);
            return await SendRequest(url, "DELETE", null);
        }

        /// <summary>
        /// Perform a POST to an RPC function.
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="json">JSON parameters</param>
        /// <returns>Response</returns>
        public async UniTask<SupabaseResponse> Rpc(string functionName, string json = null)
        {
            string url = $"{_config.RestUrl}/rpc/{functionName}";
            return await SendRequest(url, "POST", json ?? "{}");
        }

        // ============================================================
        // URL BUILDING
        // ============================================================

        private string BuildUrl(string table, string query = null)
        {
            string url = $"{_config.RestUrl}/{table}";
            if (!string.IsNullOrEmpty(query))
            {
                url += $"?{query}";
            }
            return url;
        }

        // ============================================================
        // REQUEST SENDING
        // ============================================================

        private async UniTask<SupabaseResponse> SendRequest(string url, string method, string body, Dictionary<string, string> additionalHeaders = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                // Set body if present
                if (!string.IsNullOrEmpty(body))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _config.RequestTimeoutSeconds;

                // Set headers
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", _config.AnonKey);

                // Use access token if available, otherwise use anon key for Authorization
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
                }
                else
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_config.AnonKey}");
                }

                // Additional headers
                if (additionalHeaders != null)
                {
                    foreach (var kvp in additionalHeaders)
                    {
                        request.SetRequestHeader(kvp.Key, kvp.Value);
                    }
                }

                // Send request
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception ex)
                {
                    return new SupabaseResponse
                    {
                        Success = false,
                        StatusCode = 0,
                        Error = $"Request exception: {ex.Message}",
                        Body = null
                    };
                }

                // Build response
                var response = new SupabaseResponse
                {
                    StatusCode = (int)request.responseCode,
                    Body = request.downloadHandler?.text
                };

                if (request.result == UnityWebRequest.Result.Success)
                {
                    response.Success = true;
                    Debug.Log($"[SupabaseClient] {method} {url} -> {response.StatusCode}");
                }
                else
                {
                    response.Success = false;
                    response.Error = request.error;

                    // Try to extract error message from response body
                    if (!string.IsNullOrEmpty(response.Body))
                    {
                        try
                        {
                            // Supabase error format: {"message":"...", "code":"..."}
                            if (response.Body.Contains("\"message\""))
                            {
                                int start = response.Body.IndexOf("\"message\":\"") + 11;
                                int end = response.Body.IndexOf("\"", start);
                                if (start > 10 && end > start)
                                {
                                    response.Error = response.Body.Substring(start, end - start);
                                }
                            }
                        }
                        catch { }
                    }

                    Debug.LogWarning($"[SupabaseClient] {method} {url} FAILED: {response.StatusCode} - {response.Error}");
                }

                return response;
            }
        }
    }

    /// <summary>
    /// Response from a Supabase REST API call.
    /// </summary>
    public class SupabaseResponse
    {
        /// <summary>Whether the request was successful (2xx status code)</summary>
        public bool Success;

        /// <summary>HTTP status code</summary>
        public int StatusCode;

        /// <summary>Error message if failed</summary>
        public string Error;

        /// <summary>Response body (JSON string)</summary>
        public string Body;

        /// <summary>Check if the response is a specific status code</summary>
        public bool Is(int code) => StatusCode == code;

        /// <summary>Check if 404 Not Found</summary>
        public bool IsNotFound => StatusCode == 404;

        /// <summary>Check if 401 Unauthorized</summary>
        public bool IsUnauthorized => StatusCode == 401;

        /// <summary>Check if 409 Conflict</summary>
        public bool IsConflict => StatusCode == 409;
    }
}
