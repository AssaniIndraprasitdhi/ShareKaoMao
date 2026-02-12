# ShareKaoMao

ระบบคำนวณหารค่าเหล้ากับเพื่อน ง่ายนิดเดียว

## Tech Stack

- ASP.NET Core MVC (.NET 5)
- C# 8
- Entity Framework Core 5
- PostgreSQL (Npgsql)
- Bootstrap 5 (CDN)

## Prerequisites

- [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [PostgreSQL](https://www.postgresql.org/download/)
- dotnet-ef tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## ตั้งค่า Environment Variable

Connection string ต้องตั้งผ่าน Environment Variable ชื่อ `SHAREKAOMAO_CONNECTION_STRING`

**ห้ามใส่ใน appsettings.json**

### Windows PowerShell

```powershell
$env:SHAREKAOMAO_CONNECTION_STRING = "Host=localhost;Port=5432;Database=sharekaomao;Username=postgres;Password=yourpassword"
```

### Windows CMD

```cmd
set SHAREKAOMAO_CONNECTION_STRING=Host=localhost;Port=5432;Database=sharekaomao;Username=postgres;Password=yourpassword
```

### macOS / Linux (bash/zsh)

```bash
export SHAREKAOMAO_CONNECTION_STRING="Host=localhost;Port=5432;Database=sharekaomao;Username=postgres;Password=yourpassword"
```

> **Tip:** เพิ่มลงใน `~/.bashrc` หรือ `~/.zshrc` เพื่อให้ใช้ได้ถาวร

## ขั้นตอนรัน

1. ตั้งค่า Environment Variable (ดูด้านบน)

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. สร้าง Migration:
   ```bash
   dotnet ef migrations add InitialCreate
   ```

4. อัพเดท Database:
   ```bash
   dotnet ef database update
   ```

5. รันแอพ:
   ```bash
   dotnet run
   ```

6. เปิดเบราว์เซอร์ไปที่: `https://localhost:5001` หรือ `http://localhost:5000`

## Features

- สร้างบิล ตั้งชื่อได้
- เพิ่ม/ลบ เพื่อนในบิล (ชื่อห้ามซ้ำ)
- เพิ่ม/ลบ รายการ พร้อมเลือกคนแชร์
- ตั้งค่า VAT%, Service%, ทิป
- เลือก Rounding Mode (ไม่ปัด / ปัดขึ้น 1/5/10 บาท)
- สรุปยอดต่อคนพร้อม breakdown

## โครงสร้างโปรเจกต์

```
ShareKaoMao/
├── Controllers/
│   ├── HomeController.cs
│   └── BillsController.cs
├── Models/
│   ├── Bill.cs
│   ├── Person.cs
│   ├── Item.cs
│   ├── ItemShare.cs
│   └── RoundingMode.cs
├── ViewModels/
│   ├── BillCreateViewModel.cs
│   ├── BillDetailsViewModel.cs
│   ├── AddPersonViewModel.cs
│   ├── AddItemViewModel.cs
│   └── UpdateExtrasViewModel.cs
├── Services/
│   └── BillCalculationService.cs
├── Data/
│   └── AppDbContext.cs
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   └── Bills/
│       ├── Create.cshtml
│       └── Details.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css
│   └── js/
│       └── site.js
├── Program.cs
├── Startup.cs
├── appsettings.json
├── .env.example
└── README.md
```

## Calculation Logic

1. **ItemTotal** = Price x Quantity
2. ถ้า item ถูกแชร์ N คน: แต่ละคนจ่าย = ItemTotal / N
3. **Subtotal** ต่อคน = รวม share ทั้งหมด
4. **VAT** = subtotal x VatPercent / 100
5. **Service** = subtotal x ServicePercent / 100
6. **Tip** = TipAmount x (subtotal / subtotal_รวม)
7. กรณี subtotal รวม = 0 → แจก extras เท่ากัน
8. **GrandTotal** = Subtotal + VAT + Service + Tip
9. **Rounding**: ปัดขึ้นทุกคน แล้วปรับคนสุดท้ายให้ยอดรวมตรง
