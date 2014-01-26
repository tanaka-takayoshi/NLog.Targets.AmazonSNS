NLog.Targets.AmazonSNS
======================
[![Build status](https://ci.appveyor.com/api/projects/status?id=i16eafsfedh5q3g9)](https://ci.appveyor.com/project/nlog-targets-amazonsns)

Nlog target for Amazon Simple Notification Service (SNS)

You can send log messages through Amazon Simple Notification Service (SNS) with NLog.Targets.AmazonSNS.
Suites .Net applications hosted in Amazon EC2.

## Requirements

Amazon AWS account credentials
Amazon Simple Notification Service enabled. And create one Topic of SNS. And configure at least one subscription for this topic. For example, you can use mail subscription after your mail address is verified.
Download

You can download from Nuget. Nuget site of this project is here.

## Code

License is Microsoft Public License (Ms-PL).

## Configuration

Include NLog.Targets.AmazonSNS.dll to your project and add NLog.Targets.AmazonSNS as an extension in your NLog.config.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <add assembly="NLog.Targets.AmazonSNS" />
  </extensions>
  <targets>
    <target xsi:type="BufferingWrapper" flushTimeout="60000" name="sns">
      <target name="sns-inner" xsi:type="SNSTarget"
              maxMessageSize="256"
              awsAccessKey="Paste your AWS Access Key"
              awsSecretKey="Paste your AWS Secret Key"
              endpoint="e.g. ap-northeast-1"
              topicArn="arn:aws:sns:ap-northeast-1:123456789012:Your-Notification"
              subject="[Test][SNSTarget][${level:uppercase=true}]"
              layout="[Test][${longdate}][${level:uppercase=true}]
[${logger}]${newline}${message}${newline}${exception:format=tostring}" />
    </target>
  </targets>
  <rules>
    <logger name="*" level="Error" writeTo="sns" />
  </rules>
</nlog>
```
