ALTER Proc [dbo].[prcUpdateAllOrderPositions]
As
Begin
	truncate table tblAllOrderPositions

	insert into tblAllOrderPositions (userGuid, symname, side, qty, marketValue)
	select userGuidId, symName, side, qty, marketValue
	from tblOrderPositions


	update A
	set A.dtTranIn = B.dtTranIn, A.stratName = B.StratName, A.amountIn = B.AmountIn, A.email = B.Email, A.rateIn = B.RateIn
	from tblAllOrderPositions A
	cross apply(
		select *
		from tblOrder BB
		where A.userGuid=BB.UserGUID
		and A.symName=BB.SymName
		and BB.dtTranOut is null
	)B

	select * from tblAllOrderPositions

	truncate table tblOrderPositions

End
