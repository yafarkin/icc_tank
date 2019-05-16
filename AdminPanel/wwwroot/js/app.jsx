
    class UserForm extends React.Component {
    constructor(props) {
        super(props);

        //StartServer([FromBody] int port, [FromBody] string nameGame, [FromBody] int maxBotsCount,
        //    [FromBody] int coreUpdateMs, [FromBody] int spectatorUpdateMs, [FromBody] int botUpdateMs)

        var nameGame = props.nameGame;
        var nameGameIsValid = this.validateNameGame(nameGame);
        var port = props.port;
        var portIsValid = this.validatePort(port);

        this.state = { nameGame: nameGame, port: port, nameValid: nameGameIsValid, portValid: portIsValid };

        this.onNameGameChange = this.onNameGameChange.bind(this);
        this.onPortChange = this.onPortChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
        }

        //проверить что порт больше нуля
        validatePort(port)
        {
            return port >= 0;
        }

        //проверить, чтоимя серверя больше трёх символов
        validateNameGame(nameGame)
        {
            return nameGame.length > 2;
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

        //запросить подтвердить введённые данные
        handleSubmit(e)
        {
            e.preventDefault();
            if (this.state.nameGameIsValid === true && this.state.portValid === true)
            {
                alert("Имя сервера: " + this.state.nameGame + " Порт: " + this.state.port);
            }
        }

        render()
        {
            // цвет границы для поля для ввода имени
            var nameGameColor = this.state.nameGameIsValid === true ? "green" : "red";
            // цвет границы для поля для ввода возраста
            var portColor = this.state.portValid === true ? "green" : "red";
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
                    <input type="submit" value="Отправить" />
                </form>
            );
        }
}
ReactDOM.render(
    <UserForm nameGame="" port="0" />,
    document.getElementById("app")
)