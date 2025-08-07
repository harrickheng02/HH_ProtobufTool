
è*
Proto_WS2C.protoHHFramework.ProtoProto_Common.protoProto_C2WS.proto"G
WS2C_ReturnCreateRole
Result (RResult
RoleId (RRoleId"G
WS2C_ReturnDeleteRole
Result (RResult
RoleId (RRoleId"Å
WS2C_ReturnRoleList[
RoleList (2?.HHFramework.Proto.WS2C_ReturnRoleList.WS2C_ReturnRoleList_ItemRRoleListå
WS2C_ReturnRoleList_Item
RoleId (RRoleId
JobId (RJobId
Sex (RSex
NickName (	RNickName
Level (RLevel"û
WS2C_ReturnRoleInfo
RoleId (RRoleId
JobId (RJobId
Sex (RSex
NickName (	RNickName
Level (RLevel 
CurrSceneId (RCurrSceneId 
PrevSceneId (RPrevSceneId4
CurrPos (2.HHFramework.Proto.Vector3RCurrPos
	RotationY	 (R	RotationY
CurrHP
 (RCurrHP
MaxHP (RMaxHP
CurrMP (RCurrMP
MaxMP (RMaxMP
CurrFury (RCurrFury
CurrGold (RCurrGold
Fighting (RFighting
Exp (RExpV
WearEquipList (20.HHFramework.Proto.WS2C_ReturnRoleInfo.WearEquipRWearEquipList(
CurrGameLevelId (RCurrGameLevelId.
CurrGameLevelGrade (RCurrGameLevelGrade(
PassGameLevelId (RPassGameLevelId_
	WearEquip
Type (RType
EquipId (REquipId$
GoodsServerId (RGoodsServerId"ﬂ
WS2C_ReturnGoldChange
OldGold (ROldGold
CurrGold (RCurrGold=

ChangeType (2.HHFramework.Proto.ChangeTypeR
ChangeType8
AddType (2.HHFramework.Proto.GoldAddTypeRAddTypeA

ReduceType (2!.HHFramework.Proto.GoldReduceTypeR
ReduceType:
	GoodsType (2.HHFramework.Proto.GoodsTypeR	GoodsType
GoodsId (RGoodsId"Ê
WS2C_ReturnMoneyChange
OldMoney (ROldMoney
	CurrMoney (R	CurrMoney=

ChangeType (2.HHFramework.Proto.ChangeTypeR
ChangeType9
AddType (2.HHFramework.Proto.MoneyAddTypeRAddTypeB

ReduceType (2".HHFramework.Proto.MoneyReduceTypeR
ReduceType:
	GoodsType (2.HHFramework.Proto.GoodsTypeR	GoodsType
GoodsId (RGoodsId"
WS2C_ReturnEnterGameComplete"Y
WS2C_ReturnChatMsgC
ChatMsgList (2!.HHFramework.Proto.C2WS_Chat_DataRChatMsgList"O
WS2C_PushChatMsg;
ChatMsg (2!.HHFramework.Proto.C2WS_Chat_DataRChatMsg"M
WS2C_ReturnShopBuyProduct
Result (RResult
MsgCode (RMsgCode"’
WS2C_ReturnRecharge
Result (RResult
MsgCode (RMsgCode
	ProductId (R	ProductId 
ProductType (RProductType
	RemainDay (R	RemainDay.
TotalRechargeMoney (RTotalRechargeMoney"◊
WS2C_ReturnRechargeProduct
RechargeProductList (2M.HHFramework.Proto.WS2C_ReturnRechargeProduct.WS2C_ReturnRechargeProduct_ItemRRechargeProductList∑
WS2C_ReturnRechargeProduct_Item
	ProductId (R	ProductId 
ProductDesc (	RProductDesc
CanBuy (RCanBuy
	RemainDay (R	RemainDay

DoubleFlag (R
DoubleFlag"≠
WS2C_ReturnBackpackGoodsChange
GoodsChangeList (2U.HHFramework.Proto.WS2C_ReturnBackpackGoodsChange.WS2C_ReturnBackpackGoodsChange_ItemRGoodsChangeListâ
#WS2C_ReturnBackpackGoodsChange_Item&
BackpackItemId (RBackpackItemId

ChangeType (R
ChangeType:
	GoodsType (2.HHFramework.Proto.GoodsTypeR	GoodsType
GoodsId (RGoodsId

GoodsCount (R
GoodsCount$
GoodsServerId (RGoodsServerId"˝
WS2C_ReturnSearchBackpackk

SearchList (2K.HHFramework.Proto.WS2C_ReturnSearchBackpack.WS2C_ReturnSearchBackpack_ItemR
SearchListÚ
WS2C_ReturnSearchBackpack_Item&
BackpackItemId (RBackpackItemId:
	GoodsType (2.HHFramework.Proto.GoodsTypeR	GoodsType
GoodsId (RGoodsId$
GoodsServerId (RGoodsServerId,
GoodsOverlayCount (RGoodsOverlayCount"†
WS2C_ReturnSearchEquipDetail"
EnchantLevel (REnchantLevel"
EnchantCount (REnchantCount$
BaseAttr1Type (RBaseAttr1Type&
BaseAttr1Value (RBaseAttr1Value$
BaseAttr2Type (RBaseAttr2Type&
BaseAttr2Value (RBaseAttr2Value
Hp (RHp
Mp (RMp
Atk	 (RAtk
Def
 (RDef"
CriticalRate (RCriticalRate(
CriticalResRate (RCriticalResRate2
CriticalStrengthRate (RCriticalStrengthRate
	BlockRate (R	BlockRate"
BlockResRate (RBlockResRate,
BlockStrengthRate (RBlockStrengthRate

InjureRate (R
InjureRate$
InjureResRate (RInjureResRate,
ExSkillInjureRate (RExSkillInjureRate2
ExSkillInjureResRate (RExSkillInjureResRate$
IgnoreDefRate (RIgnoreDefRate
IsPutOn (RIsPutOn"N
WS2C_ReturnSellToSys
	IsSuccess (R	IsSuccess
MsgCode (RMsgCode"f
WS2C_ReturnUseItem
	IsSuccess (R	IsSuccess
MsgCode (RMsgCode
GoodsId (RGoodsId"°
WS2C_ReturnEquipPut
	IsSuccess (R	IsSuccess
MsgCode (RMsgCode
Type (RType
GoodsId (RGoodsId$
GoodsServerId (RGoodsServerId"ï
WS2C_ReturnGameLevelSettlement 
GameLevelId (RGameLevelId&
GameLevelGrade (RGameLevelGrade
PassTime (RPassTime
	IsVictory (R	IsVictoryi
RwdLst (2Q.HHFramework.Proto.WS2C_ReturnGameLevelSettlement.WS2C_ReturnGameLevelRwdLst_DataRRwdLstÉ
WS2C_ReturnGameLevelRwdLst_Data
Id (RId:
	GoodsType (2.HHFramework.Proto.GoodsTypeR	GoodsType
Count (RCount"L
WS2C_ReturnRoleExpChange
OldExp (ROldExp
CurrExp (RCurrExp"V
WS2C_ReturnRoleLevelChange
OldLevel (ROldLevel
	CurrLevel (R	CurrLevelbproto3