var data = [
    { 'Id': 1, 'Name': 1, 'Ip': 'fgbhf', 'Port': '2000', 'Type': 'Tank' },
    { 'Id': 2, 'Name': 1, 'Ip': 'MONDAY', 'Port': '2010', 'Type': 'Tank' },
    { 'Id': 3, 'Name': 1, 'Ip': 'TUESDAY', 'Port': '2020', 'Type': 'Tank' },
    { 'Id': 4, 'Name': 1, 'Ip': 'WEDNESDAY', 'Port': '2030', 'Type': 'Tank' },
    { 'Id': 5, 'Name': 1, 'Ip': 'THURSDAY', 'Port': '2040', 'Type': 'Tank' },
    { 'Id': 6, 'Name': 1, 'Ip': 'FRIDAY', 'Port': '2050', 'Type': 'Tank' }
];

var serversSettings = [
    { 'Name': '1', 'Text': 'sdfsd', 'Value': 1 },
    { 'Name': '2', 'Text': 'sdfsd', 'Value': 1 },
    { 'Name': '3', 'Text': 'sdfsd', 'Value': 1 },
    { 'Name': '4', 'Text': 'sdfsd', 'Value': 1 },
]

class UserForm extends React.Component {

    requestHelper(type, method, data) {
        var form = new FormData();
        for (var propt in data) {
            form.append(propt, data[propt]);
        }

        var type = { method: type, body: form };
        var url = `admin/${method}`;

        fetch(url, type);
    }

    postHelper(method, data) {
        this.requestHelper("POST", method, data);
    }

    constructor(props) {
        super(props);

        //
        var port = props.port;
        var portIsValid = this.validatePort(port);
        //
        var nameGame = props.nameGame;
        var nameGameIsValid = this.validateNameGame(nameGame);
        //
        var maxBotsCount = props.maxBotsCount;
        var maxBotsCountIsValid = this.validateMaxBotsCount(maxBotsCount);
        //
        var coreUpdatesMs = props.coreUpdatesMs;
        var coreUpdatesMsIsValid = this.validateCoreUpdatesMs(coreUpdatesMs);

        this.state = {
            viewModal: false,
            nameGame: nameGame, port: port, maxBotsCount: maxBotsCount, coreUpdatesMs: coreUpdatesMs,/*
         */ nameGameValid: nameGameIsValid, portValid: portIsValid, maxBotsCountValid: maxBotsCountIsValid, coreUpdatesMsValid: coreUpdatesMsIsValid
        };

        this.onNameGameChange = this.onNameGameChange.bind(this);
        this.onPortChange = this.onPortChange.bind(this);
        this.onMaxBotsCountChange = this.onMaxBotsCountChange.bind(this);
        this.onCoreUpdateMsCharnge = this.onCoreUpdateMsCharnge.bind(this);
        //
        this.handleSubmit = this.handleSubmit.bind(this);
        this.openModal = this.openModal.bind(this);
        this.doTestStart = this.doTestStart.bind(this);
    }

    //проверить что порт больше нуля
    validatePort(port) {
        return port >= 1000;
    }

    //проверить, чтоимя серверя больше трёх символов
    validateNameGame(nameGame) {
        return nameGame.length > 2;
    }

    //проверить что введённое кол-во ботов больше одного
    validateMaxBotsCount(maxBotsCount) {
        return maxBotsCount > 1;
    }

    validateCoreUpdatesMs(coreUpdatesMs) {
        return coreUpdatesMs > 0;
    }

    //при изменении порта
    onPortChange(e) {
        var val = e.target.Port;
        var valid = this.validatePort(val);
        this.setState({ port: val, portValid: valid });
    }

    //при изменении имени сервера
    onNameGameChange(e) {
        var val = e.target.Port;
        var valid = this.validateNameGame(val);
        this.setState({ nameGame: val, nameGameIsValid: valid });
    }

    //при изменении поля максимального количества ботов
    onMaxBotsCountChange(e) {
        var val = e.target.Port;
        var valid = this.validateMaxBotsCount(val);
        this.setState({ maxBotsCount: val, maxBotsCountIsValid: valid });
    }

    onCoreUpdateMsCharnge(e) {
        var val = e.target.Port;
        var valid = this.validateCoreUpdatesMs(val);
        this.setState({ coreUpdatesMs: val, coreUpdatesMsIsValid: valid });
    }

    doTestStart() {
        var data = { maxBotsCount: 4, botUpdateMs: 100, coreUpdateMs: 100, spectatorUpdateMs: 100, port: 2000 };
        console.log(document.getElementById(serversSettings[0].Name));
        var newData = serversSettings.map(z => z.Name + ': ' + document.getElementById(z.Name).lastElementChild.getAttribute('value')).join();
        console.log(newData);

        // this.postHelper("CreateServer", data);
    }

    //запросить подтвердить введённые данные
    handleSubmit(e) {
        e.preventDefault();
        if (this.state.nameGameIsValid === true && this.state.portValid === true && this.state.maxBotsCountIsValid) {
            alert("Имя сервера: " + this.state.nameGame + " Порт: " + this.state.port + " Максимальное количество ботов: " + this.state.maxBotsCount);
            this.doTestStart();

        }
    }

    openModal() {
        this.setState({ viewModal: !this.state.viewModal });
    }

    render() {
        let list = data.map(p => {
            return (
                <tr key={p.Id}>
                    {Object.keys(p).filter(k => k !== 'Id').map(k => {
                        return (
                            <td>
                                <div>
                                    {p[k]}
                                </div>
                            </td>
                        );
                    })}
                </tr>
            );
        });
        let dgf = serversSettings.map(p => {
            return (
                <div id={p.Name}>
                    <label>{p.Text}</label>
                    <input type="text" id="Value" defaultValue={p.Value} />
                </div>
            );
        });
        // цвет границы для поля для ввода имени игры
        var nameGameColor = this.state.nameGameIsValid === true ? "green" : "red";
        // цвет границы для поля для ввода порта
        var portColor = this.state.portValid === true ? "green" : "red";
        //цвет границы для поля для ввода максимального кол-ва ботов
        var maxBotsCountsColor = this.state.maxBotsCountIsValid === true ? "green" : "red";

        return (
            <div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Ip</th>
                            <th>Port</th>
                            <th>Type</th>
                        </tr>
                    </thead>
                    <tbody>{list}</tbody>
                </table>
                <button className="btn btn-primary" onClick={this.openModal}>Add</button>
                <div id="modal" class={this.state.viewModal ? 'visible' : 'invisible'}>{dgf}
                    <button className="btn btn-primary" onClick={this.doTestStart}>Create</button></div>
            </div>
        );
    }
}
ReactDOM.render(
    <UserForm nameGame="" port="1000" maxBotsCount="3" />,
    document.getElementById("app")
)