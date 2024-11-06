USE [paronoft_FormsAndInspections]
GO
/****** Object:  StoredProcedure [dbo].[SP_SuperAdminDynamicForms]    Script Date: 11/6/2024 11:23:08 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[SP_SuperAdminDynamicForms]
	@pageNumber as int,    
	@pageSize as int
AS
BEGIN
	DECLARE @skipRows as int;     
	SET @skipRows = (@pageNumber -1) * @pageSize; 

	SELECT *, @pageNumber as 'PageNumber',@pageSize as 'pageSize', Count(*) Over() AS TotalRows
	FROM [paronoft_FormsAndInspections].[dbo].[DynamicForms]
	ORDER BY CreatedDate DESC
	OFFSET @skipRows ROWS    
	FETCH NEXT @pageSize ROWS ONLY;   
END

--EXEC [dbo].[SP_SuperAdminDynamicForms] 1, 12
