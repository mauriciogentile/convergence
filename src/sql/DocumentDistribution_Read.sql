USE [Agenda]
GO
/****** Object:  StoredProcedure [dbo].[DocumentDistribution_Read]    Script Date: 6/6/2018 12:45:36 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[DocumentDistribution_Read] @MIN_DATE DATETIME, @TOP INT = 100 AS

SELECT DISTINCT SRQ_IDENTITY, COM_ROWID, WORKFLOW_ID, SRQ_LOAN_PROCEDURE, SR.INSTANCE_ID
INTO #SERVICE_REQUEST_TMP
FROM SERVICE_REQUEST SR
WHERE 1=1 
--AND INSTANCE_ID IS NOT NULL
AND SRQ_REQUEST_TYPE = 'OP'
AND EXISTS (SELECT 1 FROM SERVICE_REQUEST_FILE SRF WHERE SRF.SRQ_ROWID = SR.SRQ_ROWID AND SRF.SRF_TRANSLATION_IND = 'N')

SELECT 1 AS [Version], 'RG-09525' AS Pipeline, 
'OP-1507-1' AS DocID, CURRENT_TIMESTAMP AS CreatedOn, CURRENT_TIMESTAMP AS DistributedOn,
12 AS CommitteeID, 'OPC' AS Committee, '59ef567d3ed2da5780a46431' AS WorkflowID,
'STD' AS [Procedure], 'DLP' AS InstanceID, 'Nice version' as [Description]
UNION
SELECT * FROM (SELECT DISTINCT TOP (@TOP) DOV_VERSION_NBR AS [Version], DV.DOV_PIPELINE_NBR AS Pipeline, 
DV.DOV_VERSION_ID AS DocID, DOV_CREATE_DATE AS CreatedOn, DOV_ACTUAL_DIST_DATE AS DistributedOn,
A.COM_ROWID AS CommitteeID, C.CDV_DESC_ENG AS Committee, A.WORKFLOW_ID AS WorkflowID,
A.SRQ_LOAN_PROCEDURE AS [Procedure], INSTANCE_ID AS InstanceID, DOV_VERSION_NAME_ENG AS [Description]
FROM DOC_VERSION DV
JOIN #SERVICE_REQUEST_TMP A ON A.SRQ_IDENTITY = DV.DOV_PIPELINE_NBR 
JOIN [CODE_VALUES] C ON CAST(A.COM_ROWID AS nvarchar) = C.CDV_CODE
WHERE LTRIM(RTRIM(DV.DOV_PIPELINE_NBR)) <> ''
AND DOV_ACTUAL_DIST_DATE IS NOT NULL
AND DOV_ACTUAL_DIST_DATE >= @MIN_DATE
AND (DV.DOV_VERSION_ID LIKE 'OP-%' OR DV.DOV_VERSION_ID LIKE 'PR-%')
AND C.COD_NAME = 'SRQ_COM_ROWID'
ORDER BY DOV_ACTUAL_DIST_DATE DESC) AS D

IF OBJECT_ID('tempdb..#SERVICE_REQUEST_TMP') IS NOT NULL 
	DROP TABLE #SERVICE_REQUEST_TMP