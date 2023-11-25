SELECT symName, calendaryear, period, revenue, 
       revenue - LAG(revenue) OVER (PARTITION BY symName ORDER BY calendaryear) AS Diff,
       ((revenue - LAG(revenue) OVER (PARTITION BY symName ORDER BY calendaryear)) / LAG(revenue) OVER (PARTITION BY symName ORDER BY calendaryear)) AS growthRate,
	   ROW_NUMBER() OVER (PARTITION BY symName ORDER BY calendaryear desc) As RowNo
INTO #Temp
FROM tblIncomeStatement
WHERE revenue> 0 
AND period='FY'
--and symName='TSLA'
ORDER BY symName, calendaryear DESC
--OFFSET 0 ROWS
--FETCH NEXT 5 ROWS ONLY

Delete FROM #Temp
Where RowNo > 5



SELECT A.SymName, B.AverageGrowthRate
FROM tblScreener A
CROSS APPLY(
	SELECT symName, AVG(growthRate)*100  AS AverageGrowthRate
	FROM #Temp BB
	--WHERE symName='NVDA'
	WHERE A.SymName=BB.symName
	GROUP BY BB.symName
) B
--SELECT symName, AVG(growthRate)*100  AS AverageGrowthRate
--FROM #Temp
----WHERE symName='NVDA'
--GROUP BY symName

DROP TABLE #Temp;
