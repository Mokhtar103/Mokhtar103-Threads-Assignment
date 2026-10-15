using OrderProcessing.Data;
using OrderProcessing.Repositories;
using OrderProcessing.Runner;
using OrderProcessing.Services;

await DataSeeder.ResetAsync();

Console.WriteLine("Seeding completed.");


JsonFileStore fileStore = new();

CustomerRepository customerRepository = new(fileStore);
ProductRepository productRepository = new(fileStore);
CouponRepository couponRepository = new(fileStore);
OrderRepository orderRepository = new(fileStore);
IncomingOrderRepository incomingOrderRepository = new(fileStore);

SyncCustomerRepository syncCustomerRepository = new(fileStore);
SyncProductRepository syncProductRepository = new(fileStore);
SyncCouponRepository syncCouponRepository = new(fileStore);

SyncOrderValidator syncValidator = new(
    syncCustomerRepository,
    syncProductRepository,
    syncCouponRepository);

OrderValidator asyncValidator = new(
    customerRepository,
    productRepository,
    couponRepository,
    orderRepository);

Phase1Runner runner = new(
    fileStore,
    incomingOrderRepository,
    orderRepository,
    syncValidator,
    asyncValidator);

await runner.RunAsync(CancellationToken.None);