# Origami
Turn HTML into structured data

[![chriscore MyGet Build Status](https://www.myget.org/BuildSource/Badge/chriscore?identifier=2e1ed033-4736-4537-9a85-1ad807bf13c3)](https://www.myget.org/)

Origami is a framework and REST API for structured data extraction from HTML. The framework is configuration driven, and it is easy to write new configurations that support new types of unstructured HTML data. 

Though Origami is provided with a number of data transformation functions that are used during data extraction (e.g get value of HTML elemnt attribute or return regex match on elemtent text etc), users of the framework may add their own transformations to easily extend the capablities when extracting content. These can be referenced as part of the transform configuration files.

## Getting Started
This repo has a directory containing some example transform configuration files to use and test with.

## Usage
This example trasform file describes the structure of a twitter post and replies.
TRANSFORM FILE: twitterPost.txt
```
{
	"_urlPatterns": ["^https?:\\/\\/twitter\\.[a-z]*\\/.*\\/status.*"],   <-- Regex describes which URL patterns this configuration is valid for
	"tweets":
	{
		"_xpath": "//div[contains(@class,'js-actionable-tweet')]",  <-- Describes Xpath selector for all tweets on the page
		"id":  <-- Will be populated with the tweet ID. The strategy for extracting the ID is dictated in the configuration of this node
		{
			"_xpath": ".",
			"_transformations":  <-- Describes one or more transformations to be applied to all data matching on the 'id' node 
			[
				{
					"_type": "GetAttributeTransformation",  <-- The transformation is extracting a named attribute and returning its value
					"_attributename": "data-item-id"  <-- Describes the name of the attribute to extract: 'data-item-id'
				}
			]
		},
		"content":  <-- A node to hold the tweet text/image content 
		{
			"text": ".//div[@class='js-tweet-text-container']",  <-- Xpath describing the location of the tweet text, relative to the parent node, from 'tweets' above
			"photo":  <-- Will hold URLs of any images found in the tweet
			{
				"_xpath": ".//div[contains(@class,'photo')]//img",
				"_transformations": 
				[
					{
						"_type": "GetAttributeTransformation",
						"_attributename": "src"
					}
				]
			}
		},
		"author":
		{
			"_xpath": ".",
			"name":
			{
				"_xpath": ".",
				"_transformations": 
				[
					{
						"_type": "GetAttributeTransformation",
						"_attributename": "data-name"
					}
				]
			},
			"screenName":
			{
				"_xpath": ".",
				"_transformations": 
				[
					{
						"_type": "GetAttributeTransformation",
						"_attributename": "data-screen-name"
					}
				]
			},
			"id":
			{
				"_xpath": ".",
				"_transformations": 
				[
					{
						"_type": "GetAttributeTransformation",
						"_attributename": "data-user-id"
					}
				]
			}
		}
	}
	
}
```

Given that twitterPost.txt is saved, the below example can be used to parse a given twitter post URL.
The correct transform file is chosen, since the URL to parse matches the regex pattern given in the file _urlPatterns property. Results are returned as an array, one result per matching transform file:

### REST API
```
void Main()
{
	string urlToTransform = "https://twitter.com/someuser/status/6738723782632829";
	var client = new WebClient();
	var parsedAsJson = client.DownloadString($"http://localhost:57531/api/WebContent/TransformUrl?url={HttpUtility.UrlEncode(urlToTransform)}");
}
```

### Direct
```
void Main()
{
	string url = "https://twitter.com/someuser/status/6738723782632829";
	var html = new WebClient().DownloadString(url);
	var extractor = new MultiExtractor("C:/DirectoryContainingTransform", "*.txt");
	var parsed = extractor.ParsePage(url, html);
}
```

For the above transform on Twitter data, the response from either the Origami REST API or framework class library will similar to below:
```
[
  {
    "Name": "twitterStatus.txt",
    "Data": {
      "tweets": [
        {
          "id": "738044313276321792",
          "content": {
            "text": "After 3 years and 75K Tesla miles driven, Model X joins our family https://www.teslamotors.com/customer-stories/road-modelx/?utm_campaign=CS_ModelX_060116&utm_source=Twitter&utm_medium=social"
          },
          "author": {
            "name": "Tesla Motors",
            "screenName": "TeslaMotors",
            "id": "13298072"
          }
        },
        {
          "id": "738050298279661568",
          "content": {
            "text": "@TeslaMotors wait how come some of the photos the S has a sunroof and others it doesn't?..."
          },
          "author": {
            "name": "Chase Butler",
            "screenName": "ChaseButlerTV",
            "id": "345810515"
          }
        },
        {
          "id": "738044370876784640",
          "content": {
            "text": "@TeslaMotors Awesome"
          },
          "author": {
            "name": "Ann Notari*Designer",
            "screenName": "annnotari13",
            "id": "834812053"
          }
        },
        [...]
      ]
    },
    "CollectionSource": "Direct"
  }
]
```

## REST Management API
There are also some utility methods exposed by the REST API.

### /api/Management/ListTransforms
Returns a list of all transform files found, and their content.

### /api/Management/ListTransforms?filter=test
As above, optionally filtered by a given string or pattern.

### /api/Management/PostTransforms?name=testing
[BODY: {Content of new transform file}]
Create or update a new transform file. 

### /api/Management/ListUrlPatterns
Returns a list of all regular expression url patterns from transform files found. May be used to decide if the client should request a transform for a URL.

### /api/Management/Version
Returns the current version and executing assembly name.

## License

This project is licensed under the MIT License.
Part of Origami is based on openscraping-lib-csharp, which is also licensed under the MIT License.