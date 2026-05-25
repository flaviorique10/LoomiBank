# 🏦 LoomiBank - Microsserviços Bancários

Este repositório contém a solução desenvolvida para o desafio técnico do LoomiBank. O sistema é composto por um back-end robusto focado no cadastro de clientes e processamento de transações financeiras, construído sobre uma arquitetura de microsserviços para garantir alta disponibilidade, escalabilidade e resiliência.

> 📋 **Acompanhamento das Atividades:** [Visualizar Board Público no Trello](https://trello.com/invite/b/6a109228246a042979c84a6c/ATTI4b4299d550a0c3b03aab103aed59ceee08C7972C/desafio-net-ambiente-bancario)

---

## 🌐 URLs de Produção (Live Demo na Azure)
A aplicação está totalmente "dockerizada" e hospedada na nuvem (Microsoft Azure) utilizando serviços gerenciados de banco de dados e mensageria.

* **Customers API Swagger:** `https://loomibank-customers-api-h0d3d3emewfkexcj.eastus-01.azurewebsites.net/index.html`
* **Transactions API Swagger:** `https://loomibank-transactions-api-v1-f6azdxe6hhe0cbgh.eastus-01.azurewebsites.net/index.html`

---

## 🏛️ Decisões de Arquitetura e Padrões

O projeto foi rigorosamente desenhado seguindo as melhores práticas do mercado de engenharia de software para garantir um código limpo, testável e manutenível.

### 1. Clean Architecture (Arquitetura Limpa)
O sistema foi dividido em camadas lógicas rigorosas (API, Application, Domain, Infrastructure). 
* **Justificativa:** Garantir que o núcleo do sistema (Domain) seja completamente agnóstico em relação a frameworks externos (como Entity Framework Core ou RabbitMQ). Isso facilita a criação de testes de unidade e permite a troca de tecnologias de infraestrutura no futuro sem impactar as regras de negócio.

### 2. CQRS (Command Query Responsibility Segregation)
Utilizamos o padrão CQRS (via MediatR) para separar as operações de leitura (Queries) das operações de escrita (Commands).
* **Justificativa:** Em um cenário bancário, a volumetria de consultas de saldo (leitura) é absurdamente maior do que a de transferências (escrita). O CQRS permite escalar e otimizar essas duas vias de forma independente, além de evitar modelos de dados inflados.

### 3. Princípios SOLID
Todo o ciclo de vida dos objetos e fluxo de dados obedece aos princípios SOLID.
* **Justificativa:** O uso intenso de Injeção de Dependência (DIP), o isolamento de interfaces (ISP) e a responsabilidade única (SRP) dos *UseCases/Handlers* garantem que o sistema esteja aberto para extensão, mas fechado para modificações (OCP), reduzindo o acoplamento e facilitando a manutenção por equipes ágeis.

---

## 🔄 Fluxograma de Comunicação dos Serviços

A arquitetura estabelece dois tipos de comunicação entre os microsserviços para garantir consistência e resiliência, suportando eventuais quedas de rede.

```mermaid
graph TD
    %% Nós do diagrama
    Client[Client / Front-end]
    API_Gateway[API Gateway / Load Balancer]
    
    subgraph Microsserviço: Transactions
        TransAPI[Transactions API]
        TransDB[("PostgreSQL<br>Transactions DB")]
    end
    
    subgraph Microsserviço: Customers
        CustAPI[Customers API]
        CustDB[("PostgreSQL<br>Customers DB")]
        RedisCache[("Redis Cache")]
    end

    RabbitMQ[("RabbitMQ<br>(Message Broker)")]

    %% Comunicação do Cliente
    Client -->|Requisição REST| API_Gateway
    API_Gateway -->|Rotas /customers| CustAPI
    API_Gateway -->|Rotas /transactions| TransAPI

    %% Comunicação Síncrona (HTTP + Polly)
    TransAPI -.->|"Comunicação Síncrona HTTP<br>com Políticas do Polly<br>(Retry/Circuit Breaker)"| CustAPI

    %% Comunicação Assíncrona (Event Driven)
    TransAPI ==>|"Publica Evento:<br>TransactionCreatedEvent"| RabbitMQ
    CustAPI ==>|"Publica Evento:<br>CustomerUpdatedEvent"| RabbitMQ
    
    RabbitMQ ==>|Consome Evento| CustAPI
    RabbitMQ ==>|Consome Evento| TransAPI

    %% Conexões com Banco
    CustAPI --- CustDB
    CustAPI --- RedisCache
    TransAPI --- TransDB

    %% Estilos
    classDef sync fill:#f9f,stroke:#333,stroke-width:2px,stroke-dasharray: 5 5;
    classDef async fill:#bbf,stroke:#333,stroke-width:2px;
    classDef db fill:#fdb,stroke:#333,stroke-width:2px;
    
    class TransDB,CustDB,RedisCache db;
