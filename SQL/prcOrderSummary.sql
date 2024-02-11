ALTER PROCEDURE [dbo].[prcActiveOrderStatus] @userId varchar(50)
AS
BEGIN
	Delete From tblActiveOrderStatus
	Where UserId=@userId
	Insert Into [dbo].[tblActiveOrderStatus](UserId, Email, dtDate, SymName, dtTranOut, TranType, RateIn, QtyIn, AmountIn,
		TotalPL, currentPrice, currentValue, stratName, orderStatus)
	SELECT A.UserGUID, A.Email, A.dtTranIn, A.SymName, A.dtTranOut, A.TranType, A.RateIn, A.QtyIn, A.AmountIn,
		--calculating profit and loss
		ROUND(
			Case 
				When A.dtTranOut Is not null and A.TranType = 'long' THEN A.AmountOut - A.AmountIn
				When A.dtTranOut Is not null and A.TranType = 'short' THEN A.AmountIn - A.AmountOut
				When A.dtTranOut Is null and A.TranType = 'long' THEN (B.[Close] * A.QtyIn) - A.AmountIn
				When A.dtTranOut Is null and A.TranType = 'short' THEN A.AmountIn - (B.[Close] * A.QtyIn)
			End, 2
		) TotalPL,
		--Current Price
		Case When A.dtTranOut is null then B.[Close] Else A.RateOut	End lastRate,
		--Current Value
		Case When A.dtTranOut is null then B.[Close]*A.QtyIn else A.AmountOut End marketValue,
		--Strategy Name
		A.StratName,
		--Order Status
		Case When A.dtTranOut Is not Null then 'Closed' Else 'Active' End OrderStatus
	FROM [dbTrade].[dbo].[tblOrder] A
	--Getting the last close price of the symbol
	CROSS APPLY (
			SELECT TOP 1 SymName, [Close]
			FROM [P3Data].[dbo].[tblHistoryData] BB
			WHERE A.SymName = BB.SymName
			ORDER BY BB.dtDate DESC
		) B
	Where UserGUID=@userId
	
END
