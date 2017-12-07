docker-compose down
docker-compose up -d


curl -XPUT -u <username> 'http://<elasticsearch-endpoint>:9200/_xpack/license?acknowledge=true' -H "Content-Type: application/json" -d @elasticsearch-license.json


PUT http://<elasticsearch-endpoint>:9200/_templates/logstash
{
    "template" : "logstash-*",
    "mappings" : {
      "logevent" : {
         "properties": {
            "fields": {
              "properties": {
                "MetricAverage": { "type": "float" },
                "MetricMin": { "type": "float" },
                "MetricMax": { "type": "float" },
                "MetricMedian": { "type": "float" },
                "MetricSum": { "type": "float" },
                "MetricStdDev": { "type": "float" },
                "MetricP25": { "type": "float" },
                "MetricP50": { "type": "float" },
                "MetricP75": { "type": "float" },
                "MetricP95": { "type": "float" },
                "MetricP99": { "type": "float" },
                "OperationLatency": { "type": "float" },
                "Throughput": { "type": "float" }
              }
            }
         }
      }
    }
  }
}