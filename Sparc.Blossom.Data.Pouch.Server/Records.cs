using Microsoft.AspNetCore.Mvc;

namespace Sparc.Blossom.Data.Pouch.Server;

public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
public record GetDifferencesRequest(string DatasetId, Dictionary<string, List<string>> Revisions);
public record MissingItems(List<string> Missing);
public record GetChangesRequest(string PartitionKey, List<string> doc_ids, string since, int? limit);
public record GetChangesResponse(string last_seq, List<GetChangesResult> results);
public record GetChangesResult(List<GetChangesRev> rev, string id, string seq);
public record GetChangesRev(string rev);
public record GetCheckpointRequest(string PartitionKey, string Id);
public record SaveCheckpointRequest(string DatasetId, string DocumentId, [FromBody] ReplicationLog Log);

public record BulkPostDataRequest(string PartitionKey, List<dynamic> Docs);

public record GetDatumRequest(string PartitionKey, string Id);

public record SaveDatumRevisionRequest(string PartitionKey, string Id, [FromBody] dynamic Datum);

public record ServerMetadataVendor(string name, string version);
public record GetServerMetadataResponse(string couchdb, string uuid, ServerMetadataVendor vendor, string version);