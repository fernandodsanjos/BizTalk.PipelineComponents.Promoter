# BizTalk.PipelineComponents.Promoter

The pipeline component uses custom annotation tags in BizTalk schemas to be able to promote properties in situations when it is not allowed by BizTalk.<br/>

## Custom annotation
http://biztalk.shared/property/ns is used as a custom annotation namespace, http://schemas.microsoft.com/BizTalk/2003 is used internally by BizTalk.
This namespace must be added manually at the top of the BizTalk schema used for promotion, like xmlns:x="http://biztalk.shared/property/ns".

<p>You must use and import property schemas as usual</p>

<b>Sample annotation</b><br/>
```xslt
<xs:annotation>
      <xs:appinfo>
        <x:properties xmlns:x="http://biztalk.shared/property/ns">
          <x:property name="ns1:ID" xpath="/*[local-name()='MotherOf_x0020_ALLRoots' and namespace-uri()='http://XSLTransform.Schema.Schema']/*[local-name()='Record' and namespace-uri()='']/*[local-name()='ID' and namespace-uri()='']" />
          <x:property name="ns1:MsgID" xpath="/*[local-name()='MotherOf_x0020_ALLRoots' and namespace-uri()='http://XSLTransform.Schema.Schema']/*[local-name()='Record' and namespace-uri()='']/*[local-name()='MessageID' and namespace-uri()='']" />
          <x:property name="ns1:IsDirty" xpath="/*[local-name()='MotherOf_x0020_ALLRoots' and namespace-uri()='http://XSLTransform.Schema.Schema']/*[local-name()='Record' and namespace-uri()='']/@*[local-name()='IsDirty' and namespace-uri()='']" />
        </x:properties>
      </xs:appinfo>
    </xs:annotation>
```
<br/>
<p>TIP: Promote as usual in BizTalk schema editor and then change the properties and property prefixes</p>

