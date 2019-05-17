
    class UserForm extends React.Component {
    constructor(props) {
        super(props);

        //StartServer([FromBody] int port, [FromBody] string nameGame, [FromBody] int maxBotsCount,
        //    [FromBody] int coreUpdateMs, [FromBody] int spectatorUpdateMs, [FromBody] int botUpdateMs)
                            //тики сервера              частота обновления клиента          частота обновления ботов

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

        this.state = { nameGame: nameGame, port: port, maxBotsCount: maxBotsCount, coreUpdatesMs: coreUpdatesMs,/*
         */ nameGameValid: nameGameIsValid, portValid: portIsValid, maxBotsCountValid: maxBotsCountIsValid, coreUpdatesMsValid: coreUpdatesMsIsValid };

        this.onNameGameChange = this.onNameGameChange.bind(this);
        this.onPortChange = this.onPortChange.bind(this);
        this.onMaxBotsCountChange = this.onMaxBotsCountChange.bind(this);
        this.onCoreUpdateMsCharnge = this.onCoreUpdateMsCharnge.bind(this);
        //
        this.handleSubmit = this.handleSubmit.bind(this);
        }

        //проверить что порт больше нуля
        validatePort(port)
        {
            return port >= 1000;
        }

        //проверить, чтоимя серверя больше трёх символов
        validateNameGame(nameGame)
        {
            return nameGame.length > 2;
        }

        //проверить что введённое кол-во ботов больше одного
        validateMaxBotsCount(maxBotsCount)
        {
            return maxBotsCount > 1;
        }

        validateCoreUpdatesMs(coreUpdatesMs)
        {
            return coreUpdatesMs > 0;
        }

        //при изменении порта
        onPortChange(e)
        {
            var val = e.target.value;
            var valid = this.validatePort(val);
            this.setState({ port: val, portValid: valid });
        }

        //при изменении имени сервера
        onNameGameChange(e)
        {
            var val = e.target.value;
            console.log(val);
            var valid = this.validateNameGame(val);
            this.setState({ nameGame: val, nameGameIsValid: valid });
        }

        //при изменении поля максимального количества ботов
        onMaxBotsCountChange(e)
        {
            var val = e.target.value;
            var valid = this.validateMaxBotsCount(val);
            this.setState({ maxBotsCount: val, maxBotsCountIsValid: valid });
        }

        onCoreUpdateMsCharnge(e)
        {
            var val = e.target.value;
            var valid = this.validateCoreUpdatesMs(val);
            this.setState({ coreUpdatesMs: val, coreUpdatesMsIsValid: valid });
        }

        doTestStart()
        {
            axios.post(`api/values`, { 2000, "Game1", 2 , 10, 10 ,10 })
           // fetch('api/values', { method: 'post' })
        }

        //запросить подтвердить введённые данные
        handleSubmit(e)
        {
            e.preventDefault();
            if (this.state.nameGameIsValid === true && this.state.portValid === true && this.state.maxBotsCountIsValid)
            {
                //alert("Имя сервера: " + this.state.nameGame + " Порт: " + this.state.port + " Максимальное количество ботов: " + this.state.maxBotsCount);
                doTestStart(this.state.nameGame, this.state.port, this.state.maxBotsCount, this.state.coreUpdatesMs, 100, 100);
            }
        }

        render()
        {
            // цвет границы для поля для ввода имени игры
            var nameGameColor = this.state.nameGameIsValid === true ? "green" : "red";
            // цвет границы для поля для ввода порта
            var portColor = this.state.portValid === true ? "green" : "red";
            //цвет границы для поля для ввода максимального кол-ва ботов
            var maxBotsCountsColor = this.state.maxBotsCountIsValid === true ? "green" : "red";

            return (
                <form onSubmit={this.handleSubmit}>
                    <p>
                        <label>Имя сервера:</label><br />
                        <input type="text" value={this.state.nameGame}
                            onChange={this.onNameGameChange} style={{ borderColor: nameGameColor }} />
                    </p>
                    <p>
                        <label>Порт:</label><br />
                        <input type="number" value={this.state.port}
                            onChange={this.onPortChange} style={{ borderColor: portColor }} />
                    </p>
                    <p>
                        <label>Максимальное кол-во ботов:</label><br />
                        <input type="number" value={this.state.maxBotsCount}
                            onChange={this.onMaxBotsCountChange} style={{ borderColor: maxBotsCountsColor }} />
                    </p>

                    <input type="submit" value="Отправить" />
                </form>
            );
        }
}
ReactDOM.render(
    <UserForm nameGame="" port="1000" maxBotsCount="3" />,
    document.getElementById("app")
)