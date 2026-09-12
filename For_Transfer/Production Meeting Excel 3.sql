CREATE TABLE MM_PM_Indicator_Master (
    Row_ID INT IDENTITY(1,1) PRIMARY KEY,   -- Auto-increment row id
    IndicatorID VARCHAR(10) NOT NULL,       -- S, Q, C, etc.
    IndicatorName VARCHAR(100) NOT NULL     -- Safety, Quality, Cost...
);

CREATE TABLE MM_PM_Unit_Master (
    UnitID INT IDENTITY(1,1) PRIMARY KEY,
    UnitName VARCHAR(50) NOT NULL          -- Nos, %, Rs/Veh, etc.
);
CREATE TABLE MM_PM_Model_Master (
    ModelID INT IDENTITY(1,1) PRIMARY KEY,
    ModelName VARCHAR(50) NOT NULL         -- XUV300, XUV3XO, XUV400
);
  
CREATE TABLE MM_PM_KPI_Master (
    KPI_ID INT IDENTITY(1,1) PRIMARY KEY,
    Indicator_ID INT NOT NULL,
    Unit_ID INT NOT NULL,
	Description varchar(max),  
);
 
CREATE TABLE MM_KPI_Transaction
(
    Row_ID INT IDENTITY(1,1) PRIMARY KEY,

    EntryDate DATE NOT NULL,
    WeekNo INT,
    MonthNo INT,
    YearNo INT,

    KPI_ID INT NOT NULL,
    ModelID INT NULL,

    F26_Value DECIMAL(18,2),
    F27_Value DECIMAL(18,2),

    WeekValue DECIMAL(18,2),
    MonthCumValue DECIMAL(18,2),
    YTDValue DECIMAL(18,2),

    Remarks VARCHAR(MAX),

    Inserted_Date DATETIME NOT NULL DEFAULT GETDATE(),
    Inserted_User_ID NUMERIC(18,0) NULL,

    Updated_Date DATETIME NULL,
    Updated_User_ID NUMERIC(18,0) NULL
);
GO

INSERT INTO MM_PM_Indicator_Master VALUES
('S', 'Safety'),
('Q', 'Quality'),
('C', 'Cost'),
('D', 'Delivery'),
('P', 'Production'),
('SUST', 'Sustainability'),
('M', 'Morale'),
('O', 'Others');
 
INSERT INTO MM_PM_Unit_Master (UnitName) VALUES
('Nos'),
('%'),
('Rs./Veh'),
('Ltrs/Veh'),
('Units/Veh'),
('Nos/Shift'),
('Rs. Lacs'),
('Nos/Month'),
('No. of Stages'); 

INSERT INTO MM_PM_Model_Master (ModelName) VALUES
('XUV300'),
('XUV3XO'),
('XUV400');


select * from MM_PM_Indicator_Master
select * from MM_PM_Unit_Master
select * from MM_PM_Model_Master 

INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (1, 1, 'First Aid'),
       (1, 1, 'Near Miss incidence'),
       (1, 1, 'Fire Incidence');

	   INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (2, 1, 'Offline Rework RPT XUV3XO'),
       (2, 1, 'Offline Rework RPT XUV400'), 
       (2, 6, 'Buyoff Manpower Deployed'),
       (2, 6, 'Rework Manpower Deployed'),
       (2, 2, 'Process wise Zero Defect Stages *');


	   INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (3, 3, 'Scrap Cost Process'),
       (3, 7, 'Repairs & Maint. Cost Saving'),
       (3, 6, 'Contract Labour'),
       (3, 2, 'Traceability');

	   INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (4, 2, 'Schedule Adh.'),
       (4, 1, 'A Rank Breakdown'),
       (4, 2, 'Minor Stoppages'),
       (4, 2, 'Straight Pass Ratio'),
       (4, 1, 'Non RFD Veh > 5 days');

	   INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (5, 1, 'Eq.veh/man/year');


INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (6, 4, 'Water'),
       (6, 5, 'Power');

INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (7, 1, 'Recognition of Associates through JIGYASA Portal'),
       (7, 9, 'Ergonomy Status');

	   INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description)
VALUES (8, 1, 'Highlights/Lowlights/Others'),
       (8, 1, 'CCTV Camera Working Status');

	   

select * from MM_PM_Indicator_Master
select * from MM_PM_Unit_Master
select * from MM_PM_KPI_Master

select * from MM_KPI_Transaction


  
  ALTER TABLE MM_PM_KPI_Master ADD DisplayOrder INT;
 


 
 CREATE PROCEDURE usp_GetKPIData
AS
BEGIN
    SELECT 
	km.KPI_ID,
        im.IndicatorID,
        im.IndicatorName,
        km.KPI_ID,
        km.Description,
        um.UnitName
    FROM MM_PM_KPI_Master km
    JOIN MM_PM_Indicator_Master im
        ON km.Indicator_ID = im.Row_ID
    JOIN MM_PM_Unit_Master um
        ON km.Unit_ID = um.UnitID
		ORDER BY im.Row_ID ,km.DisplayOrder 
END 




insert into MM_KPI_Transaction values (GETDATE()-2,24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE()-2,256,258)
insert into MM_KPI_Transaction values (GETDATE()-2,24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE()-2,256,258)

insert into MM_KPI_Transaction values (GETDATE()-3,24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE()-3,256,258)
insert into MM_KPI_Transaction values (GETDATE()-3,24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE()-3,256,258)

insert into MM_KPI_Transaction values (GETDATE(),24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE(),256,258)
insert into MM_KPI_Transaction values (GETDATE(),24,6,2026,3,1,260,2000,20000,'REMARKS',GETDATE(),256,258)




select * from MM_PM_Indicator_Master
select * from MM_PM_Unit_Master
select * from MM_PM_KPI_Master

select mpkm.KPI_ID, mpim.IndicatorID,mpim.IndicatorName,mpum.UnitName,mpkm.Description   from MM_PM_KPI_Master mpkm
join MM_PM_Indicator_Master mpim on mpkm.Indicator_ID = mpim.Row_ID
join MM_PM_Unit_Master mpum on mpkm.Unit_ID = mpum.UnitID 

 
 

CREATE TABLE MM_KPI_Transaction_Log
(
    Log_ID INT IDENTITY(1,1) PRIMARY KEY,
    KPI_ID INT NOT NULL,
    Column_Name VARCHAR(100) NOT NULL,
	KPI_Description VARCHAR(500) NULL,
    Old_Value VARCHAR(MAX) NULL,
    New_Value VARCHAR(MAX) NULL,
    Action_Type VARCHAR(20) NOT NULL, 
    Changed_By NUMERIC(18,0) NULL,
    Changed_Date DATETIME NOT NULL DEFAULT(GETDATE()),
    EntryDate DATE NULL,
    Transaction_Row_ID INT NULL
); 
select *from MM_KPI_Transaction 
select *from MM_KPI_Transaction_Log 


SELECT COUNT(*) as CLM_CCNT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME='MM_KPI_Transaction'


