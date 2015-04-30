UNDER DEVELOPMENT NOT YET RELEASED

# LinearLogFlow
LinearLogFlow is a service that runs on your servers and sends all your json-formatted log files to [Elasticsearch](https://www.elastic.co/).

Every line in the log files are expected to be a json object.

__Example__ of a log file
``` json
{"@timestamp":"2015-04-22T15:02:45+02:00","level":"Info","message":"Starting","version":"1.0"}
{"@timestamp":"2015-04-22T15:02:46+02:00","level":"Info","message":"Found 5 servers", "servers":5}
{"@timestamp":"2015-04-22T15:02:48+02:00","level":"Info","message":"User logged on","user":"HCanber"}
```
That's it. The line can contain anything as long as it's a valid json object. LinearLogFlow will take everything on the line and push it to [Elasticsearch](https://www.elastic.co/)

<!-- TOC depth:6 withLinks:1 updateOnSave:0 -->
- [LinearLogFlow](#linearlogflow)
	- [ELK Stack](#elk-stack)
	- [SELK stack](#selk-stack)
	- [Install](#install)
		- [Start & Stop](#start-stop)
		- [Uninstall](#uninstall)
		- [Help](#help)
	- [Configuration](#configuration)
		- [Index Names](#index-names)
		- [Change timestamp property](#change-timestamp-property)
		- [Encoding](#encoding)
		- [TTL – Time To Live](#ttl-time-to-live)
		- [Include `@source`](#include-source)
		- [Mapping](#mapping)
		- [Index template](#index-template)
		- [Posting to a cluster](#posting-to-a-cluster)
		- [Posting different logs to different servers](#posting-different-logs-to-different-servers)
		- [Post multiple log files to the same index type](#post-multiple-log-files-to-the-same-index-type)
	- [Configuration – Reference](#configuration-reference)
- [LogFlow](#logflow)

<!-- /TOC -->


## ELK Stack
In the linux world the [ELK Stack ](https://www.elastic.co/products) is well known. It's a setup combining [Elasticsearch](https://www.elastic.co/) for indexing log files, [Logstash](https://www.elastic.co/products/logstash) for transforming and pushing logs into Elasticsearch, and [Kibana](https://www.elastic.co/products/kibana) for analysing and visualizing the data.

In the .Net world, Logstash can be replaced with [LogFlow](https://github.com/LogFlow/LogFlow). In LogFlow the configuration and the transformation of log files are transformed by .Net code that you have to write. Excellent choice for transforming IIS log files for example.

But if your log files already are in the correct json format you shouldn't need to write any transformation code, and this is where LinearLogFlow comes in. It pushes your log lines as-is to Elasticsearch.

## SELK stack
The absolutely easiest way to write json log files is using [Serilog](serilog.net). So the SELK stack is: Serilog, Elasticsearch, LinearLogFlow, Kibana

## Install
.Net Framework is required on the server.

1. Download LinearLogFlow
1. Copy it to the a directory on the server
1. Edit `logs.config` and `Nlog.config`, see below
1. Execute `LogFlow.exe install`. Add `--sudo` if you don't have administrator privileges.
1. Start the service `LogFlow.exe start` or manually in the Services Manager (run `services.msc`)

### Start & Stop
When LinearLogFlow has been installed as described above, you can start and stop the service by executing
- `LogFlow.exe start`
- `LogFlow.exe stop`
- or manually in the Services Manager (run `services.msc`)

### Uninstall
`LogFlow.exe uninstall`. Add `--sudo` if you don't have administrator privileges. 

### Help
`LogFlow.exe help`
See [TopShelf Documentation](http://docs.topshelf-project.com/en/latest/overview/commandline.html) for more information.


## Configuration
The `logs.config` file is where to specify the [Elasticsearch](https://www.elastic.co/) server to connect to, and where the log files can be found.

__Example__ of a `logs.config` file
``` xml
<config>
  <server uri="http://elasticserver:9200">
    <index indexName="log-{yyyyMM}">
      <log type="serviceA" path="C:\ServiceA\logs\log-*.txt" />
      <log type="systemX"  path="D:\SystemX\log-*.txt" />
    </index>
  </server>
</config>
```

In the example above, two types of log files for two different systems will be collected and sent to the specified [Elasticsearch](https://www.elastic.co/) server. The index name is based on each lines timestamp field (by default it is `@timestamp`´) so we will get a new index every month. The logs for `serviceA` will be indexed with the `_type` field set to `serviceA` and the logs for `systemX` will have `_type` set to `systemX`.

### Index Names
<a id="index_indexName"></a>
Index names can either be fixed, like `indexName="log"`, meaning all logs will end up in the same index (not recommended).
``` xml
<index indexName="log">
```
Or based on each line's timestamp by specifying a [custom date format string](https://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=vs.110%29.aspx) between `{...}`
``` xml
<index indexName="log-{yyyyMM}">
```
`log-yyyyMM` will create a new index every month.


### Change timestamp property
<a id="log_timestamp"></a>
If the `indexName` contains a [custom date format string](https://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=vs.110%29.aspx), for example `log-{yyyyMM}` then every line must contain a timestamp property. By default `@timestamp`, `timestamp`, `datetime`, `date`, `time` will be used (in that order and in any casing). So if a line in your log file looks like below you do not need to change anything.
``` json
{"@timestamp":"2015-04-22T15:02:45+02:00","level":"Info","message":"Starting","version":"1.0"}
```

To specify another timestamp property, for example `"thedate"`, set `timestamp="thedate"`:

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" timestamp="thedate" />
```

With this setting you're now able to index lines like this:

``` json
{"thedate":"2015-04-22T15:02:45+02:00","level":"Info","message":"Starting","version":"1.0"}
```

Use `|` as a separator to specify more than one field:

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" timestamp="thedate|now|@timestamp" />
```

With this setting you're now able to index lines like this:

``` json
{"now":"2015-04-22T15:02:45+02:00","level":"Info","message":"Starting","version":"1.0"}
{"@timestamp":"2015-04-22T15:02:46+02:00","level":"Info","message":"Found 5 servers", "servers":5}
{"thedate":"2015-04-22T15:02:48+02:00","level":"Info","message":"User logged on","user":"HCanber"}
```

### Encoding
<a id="log_encoding"></a>
By default the encoding for UTF-8, UTF-16 (Big and Little endian) and UTF-32 (Big and Little endian) will be detected automatically if the file starts with a [Byte Order Mark (BOM)](http://en.wikipedia.org/wiki/Byte_order_mark). If no BOM is found, UTF-8 is used. To specify another set `encoding` to one of the following values `ascii`, `utf-8`, `utf-16be`, `utf-16le` (or `unicode`), `utf-32le`, `utf-32be`, `utf-7`.

__Example__
``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" encoding="ascii" />
```

<a id="index_defaultEncoding"></a>
You may also specify a default encoding using the property `defaultEncoding` on the `index` element that applies to all child `log` elements, unless they overrides it.

__Example__
``` xml
<config>
  <server uri="http://elasticserver:9200" defaultEncoding="utf-16le">
    <index indexName="log-{yyyyMM}">
      <log type="serviceA" path="C:\ServiceA\logs\log-*.txt" encoding="ascii" />
      <log type="systemX"  path="D:\SystemX\log-*.txt" />
    </index>
  </server>
</config>
```
The encoding _UTF-16 Little Endian_ will be used for `systemX` logs and as `serviceA` has specified `encoding` _ASCII_ will be used for its files.


### TTL – Time To Live
<a id="log_ttl"></a>
If a value has been specified for `ttl`, the property `_ttl` will be set in every line sent to Elasticsearch. The format is a value followed one of the units `ms`, `s`, `h`, `d`, `w`.

__Example__
``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" ttl="4w" />
```

Every line for `serviceA` will contain `_ttl:"4w"` meaning a line will be deleted from Elasticsearch automatically after 4 weeks. By default `_ttl` is not set.

<a id="index_defaultTtl"></a>
You may also specify a default ttl using the property `defaultTtl` on the `index` element that applies to all child `log` elements, unless they overrides it.

__Example__
``` xml
<config>
  <server uri="http://elasticserver:9200" defaultTtl="5d">
    <index indexName="log-{yyyyMM}">
      <log type="serviceA" path="C:\ServiceA\logs\log-*.txt" ttl="4w" />
      <log type="systemX"  path="D:\SystemX\log-*.txt" />
    </index>
  </server>
</config>
```

Every line from `serviceA` will contain `_ttl:"4w"` and every line from `systemX` will contain `_ttl:"5d"` meaning a line will be deleted from Elasticsearch automatically after 4 weeks and 5 days, respectively.

__Note!__ If the log line already contains a `_ttl` value, it will be sent to Elasticsearch. If `ttl` on `<log ... />` or `defaultTtl` on `<index ... >` has been specified it will overwrite any existing `_ttl` value.

See [_ttl field in Elasticsearch Documentation](http://www.elastic.co/guide/en/elasticsearch/reference/current/mapping-ttl-field.html) for more information.


### Include `@source`
<a id="log_addSource"></a>
Set `addSource="true"` to include the machine name hosting the LinearLogFlow instance in a `@source` property.

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" addSource="true" />
```

If LinearLogFlow is running on the machine `ProductionServer1` then every line in ElasticSearch that LinearLogFlow posts will contain `@source: "ProductionServer1"`

### Mapping
<a id="log_mapping"></a>
To specify how properties should be indexed you may specify a mapping per type. You do this by creating a separate file and point to that in the `mapping` property.

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" mapping="mapping.json" />
```

<a id="mapping_format"></a>
The format of the mapping json file looks almost like the standard format used when PUT:ing a mapping.
Normally, the mapping format for the type `tweet` could look like this:

``` xml
// This is NOT the format used in mapping files
{
  "tweet" : {
    "properties" : {
      "message" : {
        "type" : "string",
        "store" : true
      }
    }
  }
}
```

When creating the mapping json file the `tweet` property is removed, so it becomes:

``` xml
// This IS the format used in mapping files
{
  "properties" : {
    "message" : {
      "type" : "string",
      "store" : true
    }
  }
}
```

This means that the same mapping file may be used for different types:

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" mapping="mapping.json" />
<log type="systemX"  path="D:\SystemX\log-*.txt" mapping="mapping.json" />
```

<a id="index_defaultMapping"></a>
You may also specify `defaultMapping` on the `index` element that will apply to all `log` elements unless the specify their own mapping.
``` xml
<index indexName="log-{yyyyMM}" defaultMapping="mapping.json">
  <log type="serviceA" path="C:\ServiceA\logs\log-*.txt" />
  <log type="systemX"  path="D:\SystemX\log-*.txt" mapping="systemXmapping.json" />
</index>
```

In the example above `serviceA` type will use the mapping specified in `mapping.json` while `systemX` will use `systemXmapping.json`.

__Note!__ The mapping will be put when a new index is created. It will also be put the first time LinearLogFlow writes a log line to an index, no matter if the index existed or not, after it has been restarted.

__Example__
A log line is to be inserted into index _log-201504_. As it's a new index, it's created (potentially with an index template, see [below](#index_indexTemplate)). After that, the mapping is put into the index. The log line is inserted. The LinearLogFlow is restarted. A new log line is to be inserted into the existing index _log-201504_. As it's the first time, after LinearLogFlow was started, a line is inserted into that index, the mapping will be put again.


See http://www.elastic.co/guide/en/elasticsearch/reference/current/indices-put-mapping.html and http://www.elastic.co/guide/en/elasticsearch/reference/current/mapping.html for more information on mapping in Elasticsearch.

### Index template
<a id="index_indexTemplate"></a>
An index template can be specified on the `index` element by specifying a file on the `indexTemplate` property. The file must be a json file and follow the format described in [Elasticsearch documentation](http://www.elastic.co/guide/en/elasticsearch/reference/current/indices-create-index.html).  The template will be written when a new index is created.

``` xml
<index indexName="log-{yyyyMM}" indexTemplate="template.json">
```

### Posting to a cluster
<a id="index_isCluster"></a>
Set `isCluster="true"` to specify that the specified server belongs to a cluster to be able to failover to other nodes in the cluster, if the specified server goes down.

``` xml
<server uri="http://elasticserver:9200" isCluster="true">
```

To specify more seed nodes separate them with `|`.

``` xml
<server uri="http://elasticserver1:9200|http://elasticserver2:9200" isCluster="true">
```

### Posting different logs to different servers
<a id="multiple_server"></a>
You may specify more than one `server` element, to post to different servers
``` xml
<config>
  <server uri="http://elasticserverA:9200">
    <index indexName="log-{yyyyMM}">
      <log type="serviceA" path="C:\ServiceA\logs\log-*.txt" />
    </index>
  </server>
  <server uri="http://elasticserverB:9200">
    <index indexName="log-{yyyyMM}">
      <log type="systemX"  path="D:\SystemX\log-*.txt" />
    </index>
  </server>
</config>
```
In this example files for `serviceA` will be posted to `elasticserverA` and files for `systemX` will be posted to `elasticserverB`.

### Post multiple log files to the same index type
It's possible to have several `log` elements have the same `type` property to insert logs from different locations into the same index and type. The `name` property must then be specified on at least one of the `log` elements, in order to make the `log` elements distinguishable from each other

``` xml
<log type="serviceA" path="C:\ServiceA\logs\log-*.txt" />
<log type="serviceA" name="systemX"  path="D:\SystemX\log-*.txt" />
```
The `name` property is an optional property, which defaults to the value of `type`, so in the example above, the first `log` element will have `type="serviceA"` and `name="serviceA"`. The second  `log` element will have `type="serviceA"` and `name="systemX"`.

__Note!__ Name property has no effect on Elasticsearch. It's only used internally in LinearLogFlow.

## Configuration – Reference
Configuration is specified in a xml file called `logs.config`
``` xml
<config>
  <server uri="http://localhost:9200" [isCluster="true|false"]>
    <index indexName="log-{yyyyMM}" [indexTemplate="indexTemplate.json"] [defaultEncoding="utf-8"] [defaultTtl="31d"] [defaultMapping="mapping.json"] >
      <log type="indexType" path="C:\logs\log-*.txt" [name="indexType"] [encoding="utf-8"] [ttl="31d"] [mapping="mapping.json"] [addSource="true|false"] [timestamp="@timestamp"]/>
    </index>
  </server>
</config>
```

| Element | Property |     | Description |
|:------- |:-------- |:--- |:----------- |
| `server` | | __Required__ | At least one `server` element is required. To index to different Elasticsearch servers/clusters the config file may contain more than one `server` element. [More info](#multiple_server)  |
| | `uri` | __Required__ | The uri to the server. Example: `http://localhost:9200`. For clusters specify seed nodes by separating them by <code>&#124;</code>. Example: <code>http://server1:9200&#124;http://server2:9200</code> [More info](#server_uri) |
| | `isCluster` | _Optional_ Default: `false` | Set to `true` to specify that the specified server/servers are part of a cluster. [More info](#server_isCluster) |
| `index` | | __Required__ | Every `server` element must contain at least one `index` element. More than one is allowed. |
| | `indexName` | __Requried__ | The name of the index. May use [custom date format specifiers](https://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=vs.110%29.aspx) to create new indices based on the timestamp on each log line. Example: `log-{yyyyMM}` [More info](#index_indexName) |
| | `indexTemplate` | _Optional_ | A json file containing the settings and mappings for the index  [More info](#log_indexTemplate) |
| | `defaultEncoding` | _Optional_ Default: `utf-8` | The default encoding of the log files. By default UTF-8, UTF-16 (Big and Little endian) and UTF-32 (Big and Little endian) will be detected automatically if the file starts with a [Byte Order Mark (BOM)](http://en.wikipedia.org/wiki/Byte_order_mark). If no BOM is found, UTF-8 is used. Example: `ascii` [More info](#index_defaultEncoding) |
| | `defaultTtt` | _Optional_ | If specified the value will be written in the `_ttl` property on each line/document. The format is a value followed one of the units `ms`, `s`, `h`, `d`, `w`. Example: `4w`  [More info](#index_defaultTtl)  |
| | `defaultMapping` | _Optional_ | A path to a json file containg the mapping that will be applied to all index types inserted by LinearLogFlow. Example: `mapping.json` [Mapping format](#="mapping_format) |
| `log` | | __Required__ | Every `index` element must contain at least one `log` element. More than one is allowed. Every `log` must be unique. The uniqueness is determined by the value of `name` which defaults to the value of `type`. If two or more share the same `type` value, `name` must be manually specified to ensure uniqueness. |
| | `type` | __Required__ | The index type or mapping type under which the log will be inserted. Example: `SystemA` See [Elasticsearch documentation](http://www.elastic.co/guide/en/elasticsearch/reference/current/mapping.html) |
| | `path` | __Required__ | The path to where log files can be found. May contain the wildcard `*` in the file name, but not in the directory name. Example: `C:\logs\log-*.txt`. To collect files from more than one directory, use a `log` element for every directory and set different `name` values on every element. |
| | `name` | _Optional_ Defaults to the value of `type` | The name is optional as long as all `log` elements have unique `type` values. If two or more `log` share the same `type` value then a `name` must be specified to make them distinguishable from each other. __Note__ The name is only used internally by LinearLogFlow and is never written to Elasticsearch. [More info](#log_name) |
| | `encoding` | _Optional_ Default: `utf-8` | The encoding of the log files. By default UTF-8, UTF-16 (Big and Little endian) and UTF-32 (Big and Little endian) will be detected automatically if the file starts with a [Byte Order Mark (BOM)](http://en.wikipedia.org/wiki/Byte_order_mark). If no BOM is found, UTF-8 is used. Overrides `defaultEncoding`, if it has been specified. Example: `ascii` [More info](#log_encoding) |
| | `ttl` | _Optional_ | If specified the value will be written in the `_ttl` property on each line/document. The format is a value followed one of the units `ms`, `s`, `h`, `d`, `w`. Overrides `defaultTtl`, if it has been specified. Example: `4w`  [More info](#log_ttl)  |
| | `mapping` | _Optional_ | A path to a json file containg the mapping that will be applied to the index for the specified `type`.  Overrides `defaultMapping`, if it has been specified. Example: `mapping.json` [Mapping format](#="mapping_format) |
| | `addSource` | _Optional_ Default: `false` | If set to `true` the machine name that hosts the LinearLogFlow instance will be ritten in the `@source` property. [More info](#log_addSource) |
| | `timestamp` | _Optional_ Default: `@timestamp`, `timestamp` | If `indexName` contains a [custom date format string](https://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=vs.110%29.aspx), for example `log-{yyyyMM}` then every line must contain a timestamp property. By default `@timestamp`, `timestamp`, `datetime`, `date`, `time` will be used (in that order and in any casing). Specify this if the timestamp is in another property in your log files. If the log files can contain the timestamp in different propertys, separate the names with <code>&#124;</code>. Example: <code>thedate&#124;now</code> [More info](#log_datetime) |

# LogFlow
LinearLogFlow is based on [LogFlow](https://github.com/LogFlow/LogFlow).
