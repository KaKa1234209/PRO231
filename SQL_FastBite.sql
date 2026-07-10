USE FastBiteDB;
GO

CREATE TABLE Roles
(
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(250) NULL
);

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserName VARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(15) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    RoleId INT NOT NULL,

    CONSTRAINT FK_Users_Roles
    FOREIGN KEY(RoleId) REFERENCES Roles(RoleId)
);

/* =========================
   CUSTOMERS
========================= */
CREATE TABLE Customers
(
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL UNIQUE,

    FullName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Point INT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Customers_Users
    FOREIGN KEY(UserId) REFERENCES Users(UserId)
);

/* =========================
   EMPLOYEES
========================= */
CREATE TABLE Employees
(
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL UNIQUE,

    FullName NVARCHAR(100) NOT NULL,
    Position NVARCHAR(50) NOT NULL,
    Phone VARCHAR(15) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    HireDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT N'Đang làm việc',

    CONSTRAINT FK_Employees_Users
    FOREIGN KEY(UserId) REFERENCES Users(UserId)
);

/* =========================
   CATEGORIES
========================= */
CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL
);
ALTER TABLE Categories
ADD Status BIT NOT NULL DEFAULT 1;
/* =========================
   PRODUCTS
========================= */
CREATE TABLE Products
(
    ProductId INT IDENTITY(1,1) PRIMARY KEY,

    CategoryId INT NOT NULL,

    ProductName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(255) NULL,
    Image VARCHAR(255) NULL,
    Status BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Products_Categories
    FOREIGN KEY(CategoryId) REFERENCES Categories(CategoryId)
);

/* =========================
   INVENTORY
========================= */
CREATE TABLE Inventory
(
    InventoryId INT IDENTITY(1,1) PRIMARY KEY,

    ProductId INT NOT NULL UNIQUE,

    Quantity INT NOT NULL DEFAULT 0,
    Unit NVARCHAR(20) NOT NULL,
    UpdateAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Inventory_Products
    FOREIGN KEY(ProductId) REFERENCES Products(ProductId)
);

/* =========================
   PROMOTIONS
========================= */
CREATE TABLE Promotions
(
    PromotionId INT IDENTITY(1,1) PRIMARY KEY,

    PromotionName NVARCHAR(100) NOT NULL,
    DiscountType VARCHAR(20) NOT NULL,
    DiscountValue DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Sắp diễn ra'
);

/* =========================
   PROMOTION DETAILS
========================= */
CREATE TABLE PromotionDetails
(
    PromotionDetailId INT IDENTITY(1,1) PRIMARY KEY,

    PromotionId INT NOT NULL,
    ProductId INT NOT NULL,

    CONSTRAINT FK_PromotionDetails_Promotions
    FOREIGN KEY(PromotionId) REFERENCES Promotions(PromotionId),

    CONSTRAINT FK_PromotionDetails_Products
    FOREIGN KEY(ProductId) REFERENCES Products(ProductId)
);

/* =========================
   CARTS
========================= */
CREATE TABLE Carts
(
    CartId INT IDENTITY(1,1) PRIMARY KEY,

    CustomerId INT NOT NULL UNIQUE,

    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_Carts_Customers
    FOREIGN KEY(CustomerId) REFERENCES Customers(CustomerId)
);

/* =========================
   CART ITEMS
========================= */
CREATE TABLE CartItems
(
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,

    CartId INT NOT NULL,
    ProductId INT NOT NULL,

    Quantity INT NOT NULL DEFAULT 1,
    Price DECIMAL(18,2) NOT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_CartItems_Carts
    FOREIGN KEY(CartId) REFERENCES Carts(CartId),

    CONSTRAINT FK_CartItems_Products
    FOREIGN KEY(ProductId) REFERENCES Products(ProductId)
);

/* =========================
   ORDERS
========================= */
CREATE TABLE Orders
(
    OrderId INT IDENTITY(1,1) PRIMARY KEY,

    CustomerId INT NOT NULL,

    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT N'Đang chờ xử lý',
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_Orders_Customers
    FOREIGN KEY(CustomerId) REFERENCES Customers(CustomerId)
);

/* =========================
   ORDER DETAILS
========================= */
CREATE TABLE OrderDetails
(
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,

    OrderId INT NOT NULL,
    ProductId INT NOT NULL,

    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders
    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId),

    CONSTRAINT FK_OrderDetails_Products
    FOREIGN KEY(ProductId) REFERENCES Products(ProductId)
);

/* =========================
   INVOICES
========================= */
CREATE TABLE Invoices
(
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,

    OrderId INT NOT NULL,
    EmployeeId INT NOT NULL,

    InvoiceDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL,
    Status BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Invoices_Orders
    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId),

    CONSTRAINT FK_Invoices_Employees
    FOREIGN KEY(EmployeeId) REFERENCES Employees(EmployeeId)
);

/* =========================
   INVOICE DETAILS
========================= */
CREATE TABLE InvoiceDetails
(
    InvoiceDetailId INT IDENTITY(1,1) PRIMARY KEY,

    InvoiceId INT NOT NULL,
    ProductId INT NOT NULL,

    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_InvoiceDetails_Invoices
    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId),

    CONSTRAINT FK_InvoiceDetails_Products
    FOREIGN KEY(ProductId) REFERENCES Products(ProductId)
);

INSERT INTO Roles(RoleName)
VALUES
('Admin'),
('Empolyee'),
('Customer');

ALTER TABLE Users
ADD Email VARCHAR(100) NOT NULL;

ALTER TABLE Users
ADD Phone VARCHAR(15) NOT NULL;

ALTER TABLE Employees
DROP COLUMN Email;

ALTER TABLE Employees
DROP COLUMN FullName;

ALTER TABLE Customers
DROP COLUMN FullName;

ALTER TABLE Users
ALTER COLUMN Phone VARCHAR(15) NOT NULL;

ALTER TABLE Users
ALTER COLUMN Email NVARCHAR(255) NOT NULL;

ALTER TABLE Employees
DROP COLUMN Phone;

ALTER TABLE Customers
DROP COLUMN Email;

ALTER TABLE Customers
DROP COLUMN Phone;

ALTER TABLE Customers
DROP COLUMN Status;

ALTER TABLE Users
ALTER COLUMN Email NVARCHAR(255) NOT NULL;

ALTER TABLE Users
ADD FullName NVARCHAR(100) NOT NULL DEFAULT '';

ALTER TABLE Promotions
ADD StartDate DATETIME NOT NULL DEFAULT GETDATE();

ALTER TABLE Promotions
ADD EndDate DATETIME NOT NULL DEFAULT DATEADD(DAY,30,GETDATE());

ALTER TABLE Orders
ADD DeliveryAddress NVARCHAR(255) NULL;

ALTER TABLE Orders
ADD Note NVARCHAR(500) NULL;



INSERT INTO Users
(
    UserName,
    Password,
    Email,
    Phone,
    Status,
    RoleId
)
VALUES
(
    'admin',
    '123456',
    'admin@fastbite.com',
    '0123456789',
    N'Hoạt động',
    1
);