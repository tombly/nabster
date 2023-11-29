﻿namespace Ynab.Api;

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.0.0.0 (NJsonSchema v11.0.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ScheduledTransactionsData
{
    [System.Text.Json.Serialization.JsonPropertyName("scheduled_transactions")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]   
    [System.ComponentModel.DataAnnotations.Required]
    public System.Collections.Generic.ICollection<ScheduledTransactionDetail> Scheduled_transactions { get; set; } = new System.Collections.ObjectModel.Collection<ScheduledTransactionDetail>();

    /// <summary>
    /// The knowledge of the server
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("server_knowledge")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]   
    public long Server_knowledge { get; set; } = default!;

    private System.Collections.Generic.IDictionary<string, object>? _additionalProperties;

    [System.Text.Json.Serialization.JsonExtensionData]
    public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
        set { _additionalProperties = value; }
    }
}