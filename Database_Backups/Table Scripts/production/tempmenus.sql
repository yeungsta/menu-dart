USE [DB_51435_menudart]
GO
/****** Object:  Table [dbo].[TempMenus]    Script Date: 12/29/2012 15:11:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TempMenus](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [nvarchar](max) NULL,
	[MenuId] [int] NOT NULL,
	[DateCreated] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
