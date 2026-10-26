-- ===================================================
-- ABC Retail Database
-- Author: Zenwyn Naicker
-- ===================================================

/*Code Attribution: */
/**************************************************************************************************************
 * Title: Database Design 2nd Edition
 * Author: Watt, A. and N. Eng
 * Date: 2014
 * Availability: https://collection.bccampus.ca/textbooks/database-design-2nd-edition-bccampus-96/  
 **************************************************************************************************************/

-- Users Table 
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Customer', 'Admin')),
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20),
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    LastLoginDate DATETIME2,
    IsActive BIT DEFAULT 1,
    CONSTRAINT CHK_Email CHECK (Email LIKE '%_@__%.__%')
);

-- Shopping Cart Table 
CREATE TABLE ShoppingCart (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductRowKey NVARCHAR(255) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    ImageUrl NVARCHAR(500),
    DateAdded DATETIME2 DEFAULT GETUTCDATE(),
    LastModified DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Cart_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

-- Customer Orders Table 
CREATE TABLE CustomerOrders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    OrderStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    OrderDate DATETIME2 DEFAULT GETUTCDATE(),
    DeliveryAddress NVARCHAR(255),
    Notes NVARCHAR(500),
    CONSTRAINT FK_Order_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT CHK_OrderStatus CHECK (OrderStatus IN ('Pending', 'Processing', 'Processed', 'Shipped', 'Delivered', 'Cancelled'))
);

-- Order Items Table
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductRowKey NVARCHAR(255) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18, 2) NOT NULL,
    TotalPrice AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (OrderId) REFERENCES CustomerOrders(OrderId) ON DELETE CASCADE
);

-- User credentials are intentionally excluded. Create an Admin and a Customer
-- in your own local database. Store any credential-bearing seed script in an
-- ignored *.local.sql file.

-- Display tables
SELECT * FROM Users;
SELECT * FROM ShoppingCart;
SELECT * FROM CustomerOrders;
SELECT * FROM OrderItems;
